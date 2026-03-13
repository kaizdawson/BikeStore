using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace BikeStore.API.Controllers.PayOsController;

[Route("api/payos")]
[ApiController]
public class PayOsWebhookController : ControllerBase
{
    private readonly IGenericRepository<Transaction> _tranRepo;
    private readonly IGenericRepository<OrderItem> _orderItemRepo;
    private readonly IGenericRepository<Bike> _bikeRepo;
    private readonly IGenericRepository<Order> _orderRepo;
    private readonly IGenericRepository<Listing> _listingRepo;
    private readonly IGenericRepository<User> _userRepo;
    private readonly IGenericRepository<Policy> _policyRepo;
    private readonly IUnitOfWork _uow;

    public PayOsWebhookController(
        IGenericRepository<Transaction> tranRepo,
        IGenericRepository<OrderItem> orderItemRepo,
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Order> orderRepo,
        IGenericRepository<Listing> listingRepo,
    IGenericRepository<User> userRepo,
    IGenericRepository<Policy> policyRepo,
        IUnitOfWork uow)
    {
        _tranRepo = tranRepo;
        _orderItemRepo = orderItemRepo;
        _bikeRepo = bikeRepo;
        _orderRepo = orderRepo;
        _uow = uow;
        _listingRepo = listingRepo;
        _userRepo = userRepo;
        _policyRepo = policyRepo;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(rawBody))
            return Ok(new { success = false, message = "Empty body" });

        using var doc = JsonDocument.Parse(rawBody);

        if (!doc.RootElement.TryGetProperty("data", out var data))
            return Ok(new { success = false, message = "Missing data" });

        if (!data.TryGetProperty("orderCode", out var oc))
            return Ok(new { success = false, message = "Missing data.orderCode" });

        var orderCode = oc.ValueKind == JsonValueKind.Number
            ? oc.GetInt64().ToString()
            : (oc.GetString() ?? "");

        if (string.IsNullOrWhiteSpace(orderCode))
            return Ok(new { success = false, message = "Invalid orderCode" });

        var tran = await _tranRepo.GetFirstByExpression(t => t.OrderCode == orderCode && !t.IsDeleted);
        if (tran == null)
            return Ok(new { success = false, message = "Transaction not found", orderCode });

        var status = data.TryGetProperty("status", out var st) ? st.GetString() : null;
        var code = doc.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;

        var isPaid =
            string.Equals(status, "PAID", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(code, "00", StringComparison.OrdinalIgnoreCase);

        if (!isPaid)
            return Ok(new { success = true, message = "Received but not paid", orderCode, status, code });

        var now = DateTimeHelper.NowVN();

        
        if (tran.Status == TransactionStatusEnum.Paid)
        {
            return Ok(new
            {
                success = true,
                message = "Transaction already processed",
                orderCode
            });
        }

        var order = await _orderRepo.GetFirstByExpression(o => o.Id == tran.OrderId && !o.IsDeleted);
        if (order == null)
            return Ok(new { success = false, message = "Order not found", orderCode });

        var policy = await GetCurrentActivePolicyAsync();
        if (policy == null)
            return Ok(new { success = false, message = "Active policy not found", orderCode });

        var orderItems = await _orderItemRepo.GetListByExpression(oi => oi.OrderId == tran.OrderId);
        if (orderItems == null || !orderItems.Any())
            return Ok(new { success = false, message = "Order items not found", orderCode });

        var bikeIds = orderItems
            .Select(oi => oi.BikeId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var sellerReceiveMap = new Dictionary<Guid, decimal>();
        var soldCount = 0;

        foreach (var item in orderItems)
        {
            var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == item.BikeId && !b.IsDeleted);
            if (bike == null) continue;

            var listing = await _listingRepo.GetFirstByExpression(l => l.Id == bike.ListingId && !l.IsDeleted);
            if (listing == null) continue;

            var sellerId = listing.UserId;

            var grossAmount = item.LineTotal > 0 ? item.LineTotal : item.UnitPrice;

            var sellerAmount = Math.Round(
                grossAmount * policy.PercentOfSeller / 100m,
                2,
                MidpointRounding.AwayFromZero
            );

            if (sellerReceiveMap.ContainsKey(sellerId))
                sellerReceiveMap[sellerId] += sellerAmount;
            else
                sellerReceiveMap[sellerId] = sellerAmount;

            if (bike.Status != BikeStatusEnum.Sold)
            {
                bike.Status = BikeStatusEnum.Sold;
                bike.UpdatedAt = now;
                await _bikeRepo.Update(bike);
                soldCount++;
            }
        }

        
        foreach (var kv in sellerReceiveMap)
        {
            var sellerId = kv.Key;
            var amount = kv.Value;

            var seller = await _userRepo.GetFirstByExpression(u => u.Id == sellerId && !u.IsDeleted);
            if (seller == null) continue;

            seller.WalletBalance += amount;
            seller.UpdatedAt = now;
            await _userRepo.Update(seller);
        }

        
        tran.Status = TransactionStatusEnum.Paid;
        tran.PaidAt = now;
        tran.PolicyId = policy.Id;
        tran.UpdatedAt = now;
        await _tranRepo.Update(tran);

       
        if (order.Status != OrderStatusEnum.Paid)
        {
            order.Status = OrderStatusEnum.Paid;
            order.UpdatedAt = now;
            await _orderRepo.Update(order);
        }

        await _uow.SaveChangeAsync();

        return Ok(new
        {
            success = true,
            message = "Updated to Paid + Bikes Sold + WalletBalance added for sellers",
            orderCode,
            bikeCount = bikeIds.Count,
            soldCount,
            sellerCount = sellerReceiveMap.Count,
            policyId = policy.Id
        });
    }
    private async Task<Policy?> GetCurrentActivePolicyAsync()
    {
        var now = DateTimeHelper.NowVN();

        var res = await _policyRepo.GetAllDataByExpression(
            filter: p => p.Status == PolicyStatusEnum.Active
                      && !p.IsDeleted
                      && p.AppliedDate <= now,
            pageNumber: 1,
            pageSize: 1,
            orderBy: p => p.AppliedDate,
            isAscending: false
        );

        return res.Items.FirstOrDefault();
    }
}
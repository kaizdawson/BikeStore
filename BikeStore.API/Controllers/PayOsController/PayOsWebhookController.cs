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
    private readonly IUnitOfWork _uow;

    public PayOsWebhookController(
        IGenericRepository<Transaction> tranRepo,
        IGenericRepository<OrderItem> orderItemRepo,
        IGenericRepository<Bike> bikeRepo,
        IGenericRepository<Order> orderRepo,
        IUnitOfWork uow)
    {
        _tranRepo = tranRepo;
        _orderItemRepo = orderItemRepo;
        _bikeRepo = bikeRepo;
        _orderRepo = orderRepo;
        _uow = uow;
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

        
        if (tran.Status != TransactionStatusEnum.Paid)
        {
            tran.Status = TransactionStatusEnum.Paid;
            tran.PaidAt = now;
            tran.UpdatedAt = now;
            await _tranRepo.Update(tran);
        }


       
        var order = await _orderRepo.GetFirstByExpression(o => o.Id == tran.OrderId && !o.IsDeleted);

        if (order != null && order.Status != OrderStatusEnum.Paid)
        {
            order.Status = OrderStatusEnum.Paid; 
            order.UpdatedAt = now;
            await _orderRepo.Update(order);
        }
        
        var orderItems = await _orderItemRepo.GetListByExpression(oi => oi.OrderId == tran.OrderId);

        var bikeIds = orderItems
            .Select(oi => oi.BikeId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        var soldCount = 0;

        foreach (var bikeId in bikeIds)
        {
            var bike = await _bikeRepo.GetFirstByExpression(b => b.Id == bikeId && !b.IsDeleted);
            if (bike == null) continue;

            if (bike.Status != BikeStatusEnum.Sold)
            {
                bike.Status = BikeStatusEnum.Sold;
                bike.UpdatedAt = now;
                await _bikeRepo.Update(bike);
                soldCount++;
            }
        }

        await _uow.SaveChangeAsync();

        return Ok(new
        {
            success = true,
            message = "Updated to Paid + Bikes Sold",
            orderCode,
            bikeCount = bikeIds.Count,
            soldCount
        });
    }
}
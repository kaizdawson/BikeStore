using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using BikeStore.Common.Enums;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BikeStore.Service.Implementation
{
    public class SellerDashboardService : ISellerDashboardService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public SellerDashboardService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Listing> listingRepo,
            IGenericRepository<Bike> bikeRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _orderRepo = orderRepo;
            _listingRepo = listingRepo;
            _bikeRepo = bikeRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("Không tìm thấy thông tin người dùng. Vui lòng đăng nhập lại.");
            }
            return Guid.Parse(userId);
        }

        public async Task<object> GetSellerDashboardAsync()
        {
            var sellerId = GetCurrentUserId();
            var now = DateTime.Now;
            var startOfToday = now.Date;

            var activeBikes = await _bikeRepo.GetAllDataByExpression(
                b => b.Status == BikeStatusEnum.Available
                     && b.Listing != null
                     && b.Listing.UserId == sellerId
                     && b.Listing.Status == ListingStatusEnum.Active
                     && !b.Listing.IsDeleted,
                1, 1000, null, true); 

            var pendingOrders = await _orderRepo.GetAllDataByExpression(
                o => o.OrderItems.Any(oi => oi.Bike.Listing != null && oi.Bike.Listing.UserId == sellerId)
                     && o.Status == OrderStatusEnum.Paid,
                1, 1000, null, true);

            var completedOrders = await _orderRepo.GetAllDataByExpression(
                o => o.OrderItems.Any(oi => oi.Bike.Listing != null && oi.Bike.Listing.UserId == sellerId)
                     && o.Status == OrderStatusEnum.Completed,
                1, 10000, null, true);

            var totalRevenue = completedOrders.Items?.Sum(o => o.TotalAmount) ?? 0;

            var rejectedListings = await _listingRepo.GetAllDataByExpression(
                l => l.UserId == sellerId
                     && l.Status == ListingStatusEnum.Rejected
                     && !l.IsDeleted,
                1, 1000, null, false);
             var rejectedCount = rejectedListings.Items?.Count ?? 0;


            var orderChart = new List<object>();
            for (int i = 6; i >= 0; i--)
            {
                var targetDate = startOfToday.AddDays(-i);
                var nextDate = targetDate.AddDays(1);

                var dailyOrders = await _orderRepo.GetAllDataByExpression(
                    o => o.OrderItems.Any(oi => oi.Bike.Listing != null && oi.Bike.Listing.UserId == sellerId)
                         && o.CreatedAt >= targetDate && o.CreatedAt < nextDate,
                    1, 1000, null, true);

                orderChart.Add(new
                {
                    Label = targetDate.ToString("dd/MM"),
                    Value = dailyOrders.Items?.Count ?? 0
                });
            }


            var recentOrdersResult = await _orderRepo.GetAllDataByExpression(
                o => o.OrderItems.Any(oi => oi.Bike.Listing != null && oi.Bike.Listing.UserId == sellerId),
                1, 3, o => o.CreatedAt, true); 

            var recentOrders = recentOrdersResult.Items?.Select(o => new
            {
                OrderId = o.Id,
                CustomerName = o.ReceiverName,
                Status = o.Status.ToString(),
                TotalAmount = o.TotalAmount,
                Date = o.CreatedAt.ToString("g")
            }).ToList();


            return new
            {
                Cards = new
                {
                    ActiveListings = activeBikes.Items?.Count ?? 0,
                    PendingOrders = pendingOrders.Items?.Count ?? 0,
                    TotalRevenue = totalRevenue,
                    RejectedListings = rejectedCount
                },
                OrderChart = orderChart,
                RecentOrders = recentOrders
            };
        }
    }
}
using BikeStore.Common.DTOs.Seller.Review;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Security.Claims;

namespace BikeStore.Service.Implementation
{
    public class SellerReviewService : ISellerReviewService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Order> _orderRepo;

        public SellerReviewService(
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Order> orderRepo)
        {
            _httpContextAccessor = httpContextAccessor;
            _reviewRepo = reviewRepo;
            _orderItemRepo = orderItemRepo;
            _bikeRepo = bikeRepo;
            _orderRepo = orderRepo;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new Exception("Không tìm thấy thông tin người dùng hiện tại.");
            }

            return Guid.Parse(userId);
        }

        public async Task<List<SellerReviewItemDto>> GetMyReviewsAsync()
        {
            var sellerId = GetCurrentUserId();

            var reviewRes = await _reviewRepo.GetAllDataByExpression(
                filter: x => true,
                pageNumber: 1,
                pageSize: 1000,
                orderBy: x => x.CreatedAt,
                isAscending: false
            );

            var result = new List<SellerReviewItemDto>();

            foreach (var review in reviewRes.Items)
            {
                var order = await _orderRepo.GetFirstByExpression(
                    filter: x => x.Id == review.OrderId && !x.IsDeleted,
                    includeProperties: new Expression<Func<Order, object>>[]
                    {
                        x => x.User
                    }
                );

                if (order == null)
                    continue;

                var orderItems = await _orderItemRepo.GetListByExpression(x => x.OrderId == review.OrderId);
                if (orderItems == null || !orderItems.Any())
                    continue;

                var myBikeDtos = new List<SellerReviewOrderBikeDto>();
                var isMyReview = false;

                foreach (var item in orderItems)
                {
                    var bike = await _bikeRepo.GetFirstByExpression(
                        filter: x => x.Id == item.BikeId && !x.IsDeleted,
                        includeProperties: new Expression<Func<Bike, object>>[]
                        {
                            x => x.Listing
                        }
                    );

                    if (bike?.Listing != null && bike.Listing.UserId == sellerId)
                    {
                        isMyReview = true;

                        myBikeDtos.Add(new SellerReviewOrderBikeDto
                        {
                            BikeId = bike.Id,
                            BikeName = bike.Listing.Title ?? "",
                            UnitPrice = item.UnitPrice
                        });
                    }
                }

                if (!isMyReview)
                    continue;

                result.Add(new SellerReviewItemDto
                {
                    ReviewId = review.Id,
                    OrderId = review.OrderId,
                    Rating = review.Rating,
                    Comment = review.Comment,

                    ReviewerId = order.UserId,
                    ReviewerName = order.User?.FullName ?? "",
                    ReviewerPhone = order.User?.PhoneNumber ?? "",

                    ReceiverName = order.ReceiverName ?? "",
                    ReceiverPhone = order.ReceiverPhone ?? "",
                    ReceiverAddress = order.ReceiverAddress ?? "",

                    CreatedAt = review.CreatedAt,
                    Bikes = myBikeDtos
                });
            }

            return result;
        }

        public async Task<SellerReviewSummaryDto> GetMyReviewSummaryAsync()
        {
            var sellerId = GetCurrentUserId();

            var reviewRes = await _reviewRepo.GetAllDataByExpression(
                filter: x => true,
                pageNumber: 1,
                pageSize: 1000,
                orderBy: x => x.CreatedAt,
                isAscending: false
            );

            var myRatings = new List<int>();
            var reviewOrders = new List<SellerReviewSummaryOrderDto>();

            foreach (var review in reviewRes.Items)
            {
                var order = await _orderRepo.GetFirstByExpression(
                    filter: x => x.Id == review.OrderId && !x.IsDeleted,
                    includeProperties: new Expression<Func<Order, object>>[]
                    {
                        x => x.User
                    }
                );

                if (order == null)
                    continue;

                var orderItems = await _orderItemRepo.GetListByExpression(x => x.OrderId == review.OrderId);
                if (orderItems == null || !orderItems.Any())
                    continue;

                var isMyReview = false;
                var myBikeDtos = new List<SellerReviewOrderBikeDto>();

                foreach (var item in orderItems)
                {
                    var bike = await _bikeRepo.GetFirstByExpression(
                        filter: x => x.Id == item.BikeId && !x.IsDeleted,
                        includeProperties: new Expression<Func<Bike, object>>[]
                        {
                            x => x.Listing
                        }
                    );

                    if (bike?.Listing != null && bike.Listing.UserId == sellerId)
                    {
                        isMyReview = true;

                        myBikeDtos.Add(new SellerReviewOrderBikeDto
                        {
                            BikeId = bike.Id,
                            BikeName = bike.Listing.Title ?? "",
                            UnitPrice = item.UnitPrice
                        });
                    }
                }

                if (!isMyReview)
                    continue;

                myRatings.Add(review.Rating);

                reviewOrders.Add(new SellerReviewSummaryOrderDto
                {
                    ReviewId = review.Id,
                    OrderId = review.OrderId,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,

                    ReviewerId = order.UserId,
                    ReviewerName = order.User?.FullName ?? "",
                    ReviewerPhone = order.User?.PhoneNumber ?? "",

                    ReceiverName = order.ReceiverName ?? "",
                    ReceiverPhone = order.ReceiverPhone ?? "",
                    ReceiverAddress = order.ReceiverAddress ?? "",

                    Bikes = myBikeDtos
                });
            }

            var totalReviews = myRatings.Count;
            var totalStars = myRatings.Sum();

            return new SellerReviewSummaryDto
            {
                TotalReviews = totalReviews,
                OneStar = myRatings.Count(x => x == 1),
                TwoStars = myRatings.Count(x => x == 2),
                ThreeStars = myRatings.Count(x => x == 3),
                FourStars = myRatings.Count(x => x == 4),
                FiveStars = myRatings.Count(x => x == 5),
                TotalStars = totalStars,
                AverageRating = totalReviews == 0 ? 0 : Math.Round((decimal)totalStars / totalReviews, 1),
                Orders = reviewOrders
            };
        }
    }
}
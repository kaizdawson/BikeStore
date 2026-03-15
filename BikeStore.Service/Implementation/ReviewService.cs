using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BikeStore.Service.Implement
{
    public class ReviewService : IReviewService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Review> _reviewRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReviewService(
            IUnitOfWork unitOfWork,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<Order> orderRepo,
            IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _reviewRepo = reviewRepo;
            _orderRepo = orderRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        // Hàm lấy ID đồng nhất nằm trong Service
        private Guid GetCurrentUserId()
        {
            var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null) throw new Exception("Bạn cần đăng nhập để thực hiện hành động này.");
            return Guid.Parse(claim.Value);
        }

        public async Task<bool> CreateReviewAsync(ReviewDto dto)
        {
            var currentUserId = GetCurrentUserId();

            var order = await _orderRepo.GetById(dto.OrderId);
            if (order == null) throw new Exception("Đơn hàng không tồn tại.");

            if (order.UserId != currentUserId)
                throw new Exception("Bạn không có quyền đánh giá đơn hàng của người khác.");

            if (order.Status != OrderStatusEnum.Completed)
                throw new Exception("Bạn chỉ có thể đánh giá khi đơn hàng đã hoàn tất.");

            var existing = await _reviewRepo.GetFirstByExpression(r => r.OrderId == dto.OrderId);
            if (existing != null)
                throw new Exception("Đơn hàng này đã được bạn đánh giá trước đó.");

            var review = new Review
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                Rating = dto.Rating,
                Comment = dto.Comment,
                CreatedAt = DateTimeHelper.NowVN()
            };

            await _reviewRepo.Insert(review);

            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<object?> GetMyReviewByOrderIdAsync(Guid orderId)
        {
            var currentUserId = GetCurrentUserId();

            var review = await _reviewRepo.GetFirstByExpression(
                filter: r => r.OrderId == orderId && r.Order.UserId == currentUserId
            );

            if (review == null) return null;

            return new
            {
                review.Rating,
                review.Comment,
                review.CreatedAt
            };
        }
    }
}
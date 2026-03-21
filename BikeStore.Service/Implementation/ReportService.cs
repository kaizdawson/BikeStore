using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BikeStore.Service.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ReportService(IUnitOfWork unitOfWork, IGenericRepository<Report> reportRepo, IGenericRepository<Order> orderRepo, IHttpContextAccessor httpContextAccessor)
        {
            _unitOfWork = unitOfWork;
            _reportRepo = reportRepo;
            _orderRepo = orderRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Guid.Empty;
            }

            return Guid.Parse(userId);
        }
        public async Task<(bool Success, string Message)> CreateReportAsync(ReportDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty) return (false, "Vui lòng đăng nhập.");

            var order = await _orderRepo.GetFirstByExpression(o => o.Id == dto.OrderId && o.UserId == currentUserId);
            if (order == null)
                return (false, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");

            var existed = await _reportRepo.GetFirstByExpression(r => r.OrderId == dto.OrderId && r.Status == ReportStatusEnum.Pending);
            if (existed != null)
                return (false, "Bạn đã gửi báo cáo cho đơn hàng này rồi, vui lòng chờ hệ thống xử lý.");

            var report = new Report
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                UserId = currentUserId,
                Type = dto.Type,
                Reason = dto.Reason.Trim(),
                Status = ReportStatusEnum.Pending, 
                CreatedAt = DateTimeHelper.NowVN(),
                UpdatedAt = DateTimeHelper.NowVN(),
                IsDeleted = false
            };

            await _reportRepo.Insert(report);
            var result = await _unitOfWork.SaveChangeAsync();

            return result > 0
                ? (true, "Gửi báo cáo thành công. Ban quản trị sẽ liên hệ sớm nhất.")
                : (false, "Có lỗi xảy ra khi gửi dữ liệu.");
        }
    }
}

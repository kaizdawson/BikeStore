using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Implementation
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IGenericRepository<Order> _orderRepo;

        public ReportService(IUnitOfWork unitOfWork, IGenericRepository<Report> reportRepo, IGenericRepository<Order> orderRepo)
        {
            _unitOfWork = unitOfWork;
            _reportRepo = reportRepo;
            _orderRepo = orderRepo;
        }

        public async Task<(bool Success, string Message)> CreateReportAsync(ReportDto dto)
        {
            // 1. Lấy ID người dùng hiện tại (Giả định bạn đã có hàm GetCurrentUserId)
            var currentUserId = GetCurrentUserId();
            if (currentUserId == Guid.Empty) return (false, "Vui lòng đăng nhập.");

            // 2. Kiểm tra đơn hàng có tồn tại và thuộc về Buyer này không
            // Giả sử bảng Order của bạn dùng UserId để định danh người mua
            var order = await _orderRepo.GetFirstByExpression(o => o.Id == dto.OrderId && o.UserId == currentUserId);
            if (order == null)
                return (false, "Đơn hàng không tồn tại hoặc không thuộc quyền sở hữu của bạn.");

            // 3. Kiểm tra xem đã gửi báo cáo cho đơn này chưa (Tránh spam)
            var existed = await _reportRepo.GetFirstByExpression(r => r.OrderId == dto.OrderId && r.Status == ReportStatusEnum.Pending);
            if (existed != null)
                return (false, "Bạn đã gửi báo cáo cho đơn hàng này rồi, vui lòng chờ hệ thống xử lý.");

            // 4. Tạo đối tượng Report mới
            var report = new Report
            {
                Id = Guid.NewGuid(),
                OrderId = dto.OrderId,
                UserId = currentUserId,
                Type = dto.Type,
                Reason = dto.Reason.Trim(),
                Status = ReportStatusEnum.Pending, // Trạng thái mặc định từ Enum của bạn
                CreatedAt = DateTimeHelper.NowVN(),
                UpdatedAt = DateTimeHelper.NowVN(),
                IsDeleted = false
            };

            // 5. Lưu vào Database
            await _reportRepo.Insert(report);
            var result = await _unitOfWork.SaveChangeAsync();

            return result > 0
                ? (true, "Gửi báo cáo thành công. Ban quản trị sẽ liên hệ sớm nhất.")
                : (false, "Có lỗi xảy ra khi gửi dữ liệu.");
        }

        // Hàm lấy UserID từ Token (Ví dụ)
        private Guid GetCurrentUserId() { /* Logic lấy từ Claims của bạn */ return Guid.Parse("..."); }
    }
}

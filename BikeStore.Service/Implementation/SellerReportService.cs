using BikeStore.Common.DTOs.Seller.Report;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Implementation
{
    public class SellerReportService : ISellerReportService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IUnitOfWork _uow;

        public SellerReportService(
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<Report> reportRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<User> userRepo,
            IUnitOfWork uow)
        {
            _httpContextAccessor = httpContextAccessor;
            _reportRepo = reportRepo;
            _orderItemRepo = orderItemRepo;
            _bikeRepo = bikeRepo;
            _userRepo = userRepo;
            _uow = uow;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId == null ? Guid.Empty : Guid.Parse(userId);
        }

        public async Task<List<ReportSellerItemDto>> GetMyOrderReportsAsync()
        {
            var sellerId = GetCurrentUserId();
            if (sellerId == Guid.Empty)
                throw new Exception("Vui lòng đăng nhập.");

            var reportResult = await _reportRepo.GetAllDataByExpression(
                filter: r => !r.IsDeleted,
                pageNumber: 1,
                pageSize: 1000,
                orderBy: r => r.CreatedAt,
                isAscending: false,
                includes: new Expression<Func<Report, object>>[]
                {
                    r => r.User,
                    r => r.Order
                }
            );

            var result = new List<ReportSellerItemDto>();

            foreach (var report in reportResult.Items)
            {
                var orderItems = await _orderItemRepo.GetListByExpression(x => x.OrderId == report.OrderId);
                if (orderItems == null || !orderItems.Any())
                    continue;

                var isMyOrder = false;

                foreach (var item in orderItems)
                {
                    var bike = await _bikeRepo.GetFirstByExpression(
                        filter: b => b.Id == item.BikeId && !b.IsDeleted,
                        includeProperties: new Expression<Func<Bike, object>>[]
                        {
                            b => b.Listing!
                        }
                    );

                    if (bike?.Listing != null && bike.Listing.UserId == sellerId)
                    {
                        isMyOrder = true;
                        break;
                    }
                }

                if (!isMyOrder)
                    continue;

                result.Add(new ReportSellerItemDto
                {
                    ReportId = report.Id,
                    OrderId = report.OrderId,
                    Type = report.Type,
                    Reason = report.Reason,
                    Status = report.Status,
                    ReporterId = report.UserId,
                    ReporterName = report.User?.FullName ?? "",
                    ReporterPhone = report.User?.PhoneNumber ?? "",
                    CreatedAt = report.CreatedAt,
                    UpdatedAt = report.UpdatedAt
                });
            }

            return result;
        }

        public async Task<ReportSellerItemDto> MarkProcessingAsync(Guid reportId)
        {
            var report = await GetOwnedReportAsync(reportId);

            if (report.Status != ReportStatusEnum.Pending)
                throw new Exception("Chỉ report đang Pending mới chuyển sang Processing.");

            report.Status = ReportStatusEnum.Processing;
            report.UpdatedAt = DateTimeHelper.NowVN();

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return await MapReportDtoAsync(report);
        }

        public async Task<ReportSellerItemDto> MarkResolvedAsync(Guid reportId)
        {
            var report = await GetOwnedReportAsync(reportId);

            if (report.Status != ReportStatusEnum.Processing)
                throw new Exception("Chỉ report đang Processing mới chuyển sang Resolved.");

            report.Status = ReportStatusEnum.Resolved;
            report.UpdatedAt = DateTimeHelper.NowVN();

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return await MapReportDtoAsync(report);
        }

        public async Task<ReportSellerItemDto> MarkRejectedAsync(Guid reportId)
        {
            var report = await GetOwnedReportAsync(reportId);

            if (report.Status != ReportStatusEnum.Processing)
                throw new Exception("Chỉ report đang Processing mới chuyển sang Rejected.");

            report.Status = ReportStatusEnum.Rejected;
            report.UpdatedAt = DateTimeHelper.NowVN();

            await _reportRepo.Update(report);
            await _uow.SaveChangeAsync();

            return await MapReportDtoAsync(report);
        }

        private async Task<Report> GetOwnedReportAsync(Guid reportId)
        {
            var sellerId = GetCurrentUserId();
            if (sellerId == Guid.Empty)
                throw new Exception("Vui lòng đăng nhập.");

            var report = await _reportRepo.GetFirstByExpression(
                filter: r => r.Id == reportId && !r.IsDeleted,
                includeProperties: new Expression<Func<Report, object>>[]
                {
                    r => r.User,
                    r => r.Order
                }
            );

            if (report == null)
                throw new Exception("Không tìm thấy report.");

            var orderItems = await _orderItemRepo.GetListByExpression(x => x.OrderId == report.OrderId);
            if (orderItems == null || !orderItems.Any())
                throw new Exception("Không tìm thấy sản phẩm trong đơn hàng.");

            var isMyOrder = false;

            foreach (var item in orderItems)
            {
                var bike = await _bikeRepo.GetFirstByExpression(
                    filter: b => b.Id == item.BikeId && !b.IsDeleted,
                    includeProperties: new Expression<Func<Bike, object>>[]
                    {
                        b => b.Listing!
                    }
                );

                if (bike?.Listing != null && bike.Listing.UserId == sellerId)
                {
                    isMyOrder = true;
                    break;
                }
            }

            if (!isMyOrder)
                throw new Exception("Bạn không có quyền xử lý report này.");

            return report;
        }

        private async Task<ReportSellerItemDto> MapReportDtoAsync(Report report)
        {
            User? reporter = report.User;

            if (reporter == null)
            {
                reporter = await _userRepo.GetById(report.UserId);
            }

            return new ReportSellerItemDto
            {
                ReportId = report.Id,
                OrderId = report.OrderId,
                Type = report.Type,
                Reason = report.Reason,
                Status = report.Status,
                ReporterId = report.UserId,
                ReporterName = reporter?.FullName ?? "",
                ReporterPhone = reporter?.PhoneNumber ?? "",
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            };
        }
    }
}

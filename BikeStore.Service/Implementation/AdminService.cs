using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System.Linq.Expressions;
using BikeStore.Service.Helpers;

namespace BikeStore.Service.Implementation
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IGenericRepository<User> _userRepo; 
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;
        private readonly IGenericRepository<Report> _reportRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;

        public AdminService(IUnitOfWork unitOfWork, IGenericRepository<Listing> listingRepo, IGenericRepository<User> userRepo, IGenericRepository<Bike> bikeRepo, IGenericRepository<Order> orderRepo, IGenericRepository<Transaction> transactionRepo, IGenericRepository<Report> reportRepo, IGenericRepository<OrderItem> orderItemRepo)
        {
            _unitOfWork = unitOfWork;
            _listingRepo = listingRepo;
            _userRepo = userRepo;
            _bikeRepo = bikeRepo;
            _orderRepo = orderRepo;
            _transactionRepo = transactionRepo;
            _reportRepo = reportRepo;
            _orderItemRepo = orderItemRepo;
        }

        public async Task<bool> ApproveListingAsync(Guid id, AdminApproveDto dto)
        {
            var listing = await _listingRepo.GetById(id);
            if (listing == null) throw new Exception("Không tìm thấy tin đăng.");

            if (listing.Status != ListingStatusEnum.PendingApproval)
                throw new Exception("Tin này không ở trạng thái chờ duyệt.");

            listing.Status = dto.IsApproved ? ListingStatusEnum.Active : ListingStatusEnum.Rejected;

            listing.UpdatedAt = DateTimeHelper.NowVN();

            await _listingRepo.Update(listing);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<List<object>> GetPendingListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.PendingApproval,
                pageNumber: 1,
                pageSize: 100,
                orderBy: l => l.CreatedAt, 
                isAscending: false,        
                includes: new Expression<Func<Listing, object>>[]
                {
                    l => l.User,
                    l => l.Bikes
                }
            );
            return await MapListingToResponseAsync(result.Items);
        }

        public async Task<object?> GetListingDetailAsync(Guid id)
        {
            var listing = await _listingRepo.GetFirstByExpression(
                filter: l => l.Id == id,
                includeProperties: new Expression<Func<Listing, object>>[]
                {
                    l => l.User,
                    l => l.Bikes
                }
            );

            if (listing == null) return null;

            var context = _listingRepo.GetDbContext();
            foreach (var b in listing.Bikes)
            {
                await context.Entry(b).Collection(bike => bike.Medias).LoadAsync();
                await context.Entry(b).Reference(bike => bike.Inspection).LoadAsync();
                if (b.Inspection != null)
                {
                    await context.Entry(b.Inspection).Reference(i => i.User).LoadAsync();
                }
            }

            return new
            {
                listing.Id,
                listing.Title,
                listing.Description,
                listing.Status,
                listing.CreatedAt,
                listing.UpdatedAt,

                Seller = listing.User != null ? new
                {
                    listing.User.Id,
                    listing.User.FullName,
                    listing.User.Email,
                    listing.User.PhoneNumber,
                } : null,

                Bikes = listing.Bikes.Select(b => new
                {
                    b.Id,
                    b.Category,
                    b.Brand,
                    b.Price,
                    b.Status,
                    b.FrameSize,
                    b.FrameMaterial,
                    b.Paint,
                    b.Groupset,
                    b.Operating,
                    b.TireRim,
                    b.BrakeType,
                    b.Overall,

                    Inspection = b.Inspection != null ? new
                    {
                        b.Inspection.Id,
                        InspectorName = b.Inspection.User?.FullName ?? "N/A",
                        b.Inspection.Frame,
                        b.Inspection.PaintCondition,
                        b.Inspection.Drivetrain,
                        b.Inspection.Brakes,
                        b.Inspection.Score,
                        b.Inspection.Comment,
                        b.Inspection.InspectionDate,
                        b.Inspection.CreatedAt,
                        b.Inspection.UpdatedAt
                    } : null,

                    Medias = b.Medias?.Select(m => new
                    {
                        m.Id,
                        m.Image,
                        m.VideoUrl
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<List<object>> GetInspectingListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Active && l.Bikes.Any(b => b.Status == BikeStatusEnum.PendingInspection),
                pageNumber: 1, 
                pageSize: 100,
                orderBy: l => l.CreatedAt, 
                isAscending: false,        
                includes: new Expression<Func<Listing, object>>[] 
                {
                    l => l.User,
                    l => l.Bikes
                }
            );
            return await MapListingToResponseAsync(result.Items);
        }

        public async Task<List<object>> GetActiveListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Active && l.Bikes.Any(b => b.Status == BikeStatusEnum.Available),
                pageNumber: 1, 
                pageSize: 100,
                orderBy: l => l.CreatedAt,
                isAscending: false,
                includes: new Expression<Func<Listing, object>>[] 
                {
                    l => l.User,
                    l => l.Bikes
                }
            );
            return await MapListingToResponseAsync(result.Items);
        }

        public async Task<List<object>> GetRejectedListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Rejected,
                pageNumber: 1,
                pageSize: 100,
                orderBy: l => l.CreatedAt,
                isAscending: false,
                includes: new Expression<Func<Listing, object>>[] {
                    l => l.User,
                    l => l.Bikes
                }
            );
            return await MapListingToResponseAsync(result.Items);
        }

        private async Task<List<object>> MapListingToResponseAsync(IEnumerable<Listing> items)
        {
            var context = _listingRepo.GetDbContext();
            var response = new List<object>();

            foreach (var l in items)
            {
                var firstBike = l.Bikes?.FirstOrDefault();
                var thumbnail = "";

                if (firstBike != null)
                {
                    await context.Entry(firstBike).Collection(b => b.Medias).LoadAsync();
                    await context.Entry(firstBike).Reference(b => b.Inspection).LoadAsync();
                    if (firstBike.Inspection != null)
                    {
                        await context.Entry(firstBike.Inspection).Reference(i => i.User).LoadAsync();
                    }

                    thumbnail = firstBike.Medias?
                        .OrderBy(m => m.Id)
                        .Select(m => m.Image)
                        .FirstOrDefault(img => !string.IsNullOrEmpty(img)) ?? "";
                }

                response.Add(new
                {
                    l.Id,
                    l.Title,
                    SellerName = l.User?.FullName,
                    Price = firstBike?.Price ?? 0,
                    Thumbnail = thumbnail,
                    InspectorName = firstBike?.Inspection?.User?.FullName ?? "Chưa có",
                    l.CreatedAt,
                    ListingStatus = l.Status.ToString(),
                    BikeStatus = firstBike?.Status.ToString()
                });
            }
            return response;
        }

        public async Task<(bool Success, string Message)> CreateInspectorAsync(SignUpDto dto)
        {
            var email = dto.Email.Trim().ToLower();

            var existed = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (existed != null)
                return (false, "Email này đã tồn tại trong hệ thống.");

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existedPhone = await _userRepo.GetFirstByExpression(u => u.PhoneNumber == dto.PhoneNumber);
                if (existedPhone != null)
                    return (false, "Số điện thoại này đã tồn tại.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                PhoneNumber = dto.PhoneNumber?.Trim(),
                Email = email,
                Password = PasswordHasher.Hash(dto.Password), 
                Role = RoleEnum.INSPECTOR,                    
                Status = UserStatusEnum.Active,               
                WalletBalance = 0m,
                CreatedAt = DateTimeHelper.NowVN(),
                UpdatedAt = DateTimeHelper.NowVN(),
                IsDeleted = false
            };

            await _userRepo.Insert(user);
            var result = await _unitOfWork.SaveChangeAsync();

            return result > 0
                ? (true, "Tạo tài khoản Inspector thành công và đã kích hoạt.")
                : (false, "Có lỗi xảy ra khi lưu dữ liệu.");
        }

        public async Task<object> GetUsersManagerAsync(
        string? search,
        RoleEnum? role,
        UserStatusEnum? status,
        int pageNumber,
        int pageSize)
        {
            var searchVal = search?.Trim().ToLower();

            Expression<Func<User, bool>> filter = u => !u.IsDeleted &&
                (string.IsNullOrEmpty(searchVal) || u.FullName.ToLower().Contains(searchVal) || u.Email.ToLower().Contains(searchVal)) &&
                (!role.HasValue || u.Role == role.Value) &&
                (!status.HasValue || u.Status == status.Value);

            var result = await _userRepo.GetAllDataByExpression(
                filter: filter,
                pageNumber: pageNumber,
                pageSize: pageSize,
                orderBy: u => u.CreatedAt,
                isAscending: false
            );

            int stt = (pageNumber - 1) * pageSize + 1;

            var items = result.Items.Select(u => (object)new
            {
                STT = stt++,
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role.ToString(),
                JoinedDate = u.CreatedAt.ToString("dd/MM/yyyy"),
                Status = u.Status.ToString(),
                IsLocked = u.Status == UserStatusEnum.Banned
            }).ToList();

            return new
            {
                TotalPage = result.TotalPages,
                Items = items
            };
        }

        public async Task<bool> BanUserAsync(Guid userId)
        {
            var user = await _userRepo.GetById(userId);
            if (user == null) return false;

            if (user.Status == UserStatusEnum.Banned)
            {
                user.Status = UserStatusEnum.Active;
            }
            else
            {
                user.Status = UserStatusEnum.Banned;
            }

            user.UpdatedAt = DateTimeHelper.NowVN();

            await _userRepo.Update(user);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<object> GetBrandStatisticsAsync(string? search, int pageNumber, int pageSize)
        {
            var result = await _bikeRepo.GetAllDataByExpression(
                filter: b => !b.IsDeleted,
                pageNumber: 1, pageSize: 5000, 
                includes: new Expression<Func<Bike, object>>[] { b => b.Listing }
            );

            var searchVal = search?.Trim().ToLower();

            var groups = result.Items
                .GroupBy(b => b.Brand)
                .Where(g => string.IsNullOrEmpty(searchVal) || g.Key.ToLower().Contains(searchVal))
                .ToList();

            var pagedGroups = groups
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int stt = (pageNumber - 1) * pageSize + 1;

            var items = pagedGroups.Select(g => (object)new
            {
                STT = (stt++).ToString("D2"),
                TenThuongHieu = g.Key ?? "N/A",
                TongTinDang = $"{g.Count()} tin",
                SoLuongSP = $"{g.Count(x => x.Status == BikeStatusEnum.Available && x.Listing?.Status == ListingStatusEnum.Active)} xe",
                DaBan = $"{g.Count(x => x.Status == BikeStatusEnum.Sold)} xe"
            }).ToList();

            return new
            {
                TotalPage = (int)Math.Ceiling((double)groups.Count / pageSize),
                Items = items
            };
        }

        public async Task<object> GetCategoryStatisticsAsync(string? search, int pageNumber, int pageSize)
        {
            var result = await _bikeRepo.GetAllDataByExpression(
                filter: b => !b.IsDeleted,
                pageNumber: 1, pageSize: 5000,
                includes: new Expression<Func<Bike, object>>[] { b => b.Listing }
            );

            var searchVal = search?.Trim().ToLower();

            var groups = result.Items
                .GroupBy(b => b.Category)
                .Where(g => string.IsNullOrEmpty(searchVal) || (g.Key != null && g.Key.ToLower().Contains(searchVal)))
                .ToList();

            var pagedGroups = groups
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            int stt = (pageNumber - 1) * pageSize + 1;

            var items = pagedGroups.Select(g => (object)new
            {
                STT = (stt++).ToString("D2"),
                TenLoaiXe = g.Key ?? "N/A",
                TongTinDang = $"{g.Count()} tin",
                SoLuongSP = $"{g.Count(x => x.Status == BikeStatusEnum.Available && x.Listing?.Status == ListingStatusEnum.Active)} xe",
                DaBan = $"{g.Count(x => x.Status == BikeStatusEnum.Sold)} xe"
            }).ToList();

            return new
            {
                TotalPage = (int)Math.Ceiling((double)groups.Count / pageSize),
                Items = items
            };
        }

        public async Task<object> GetDashboardOverviewAsync()
        {
            var now = DateTimeHelper.NowVN();

            var completedOrders = await _orderRepo.GetAllDataByExpression(o => o.Status == OrderStatusEnum.Completed && !o.IsDeleted, 1, 10000);
            var totalRevenue = completedOrders.Items?.Sum(o => o.TotalAmount) ?? 0;

            var allOrders = await _orderRepo.GetAllDataByExpression(o => !o.IsDeleted, 1, 10000);
            var totalTransactions = allOrders.Items?.Count ?? 0;

            var pendingListingsResult = await _listingRepo.GetAllDataByExpression(l => l.Status == ListingStatusEnum.PendingApproval && !l.IsDeleted, 1, 10000);
            var pendingListings = pendingListingsResult.Items?.Count ?? 0;

            var rejectedListingsResult = await _listingRepo.GetAllDataByExpression(l => (l.Status == ListingStatusEnum.Rejected || l.IsDeleted), 1, 10000);
            var rejectedListings = rejectedListingsResult.Items?.Count ?? 0;

            int diff = (7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime startOfCurrentWeek = now.AddDays(-diff).Date;

            var userGrowthChart = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                DateTime startDate = startOfCurrentWeek.AddDays(-(i * 7));
                DateTime endDate = startDate.AddDays(7).AddTicks(-1);

                var users = await _userRepo.GetAllDataByExpression(
                    u => u.CreatedAt >= startDate && u.CreatedAt <= endDate, 1, 10000);

                userGrowthChart.Add(new
                {
                    Label = i == 0 ? "Tuần này" : $"{i} tuần trước",
                    Range = $"{startDate:dd/MM} - {endDate:dd/MM}",
                    Value = users.Items?.Count ?? 0
                });
            }

            var revenueChart = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                DateTime startDate = startOfCurrentWeek.AddDays(-(i * 7));
                DateTime endDate = startDate.AddDays(7).AddTicks(-1);

                var weeklyOrders = await _orderRepo.GetAllDataByExpression(
                    o => o.Status == OrderStatusEnum.Completed
                         && o.CreatedAt >= startDate
                         && o.CreatedAt <= endDate, 1, 10000);

                var weeklyRevenue = weeklyOrders.Items?.Sum(o => o.TotalAmount) ?? 0;

                revenueChart.Add(new
                {
                    Label = i == 0 ? "Tuần này" : $"{i} tuần trước",
                    Range = $"{startDate:dd/MM} - {endDate:dd/MM}",
                    Value = weeklyRevenue
                });
            }

            return new
            {
                Cards = new
                {
                    TotalRevenue = totalRevenue,
                    TotalTransactions = totalTransactions,
                    PendingListings = pendingListings,
                    RejectedListings = rejectedListings
                },
                UserGrowthChart = userGrowthChart,
                RevenueWeeklyChart = revenueChart
            };
        }

        public async Task<object> GetTransactionsForAdminAsync()
        {
            var transactions = await _transactionRepo.GetAllDataByExpression(
                t => !t.IsDeleted && t.OrderId != null && t.PolicyId != null,
                1,
                int.MaxValue,
                t => t.CreatedAt,
                true,
                t => t.Policy
            );

            if (transactions.Items == null || !transactions.Items.Any())
            {
                return new
                {
                    Transactions = new List<AdminTransactionDto>(),
                    TotalSystemFee = 0
                };
            }

            decimal totalSystemFee = 0; 

            var transactionList = transactions.Items.Select(t => {
                decimal percent = t.Policy?.PercentOfSystem ?? 0;
                decimal feeAmount = t.Amount * (percent / 100);

                totalSystemFee += feeAmount;

                return new AdminTransactionDto
                {
                    TransactionId = t.Id,
                    OrderId = t.OrderId.Value,
                    CreatedAt = t.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                    TotalAmount = t.Amount,
                    SystemFee = feeAmount,
                    AppliedPercent = $"{percent}%",
                    Status = t.Status.ToString()
                };
            }).ToList();

            return new
            {
                TotalSystemFee = totalSystemFee, 
                TransactionCount = transactionList.Count, 
                Data = transactionList 
            };
        }

        public async Task<object> GetReportsForAdminAsync()
        {
            var reportsResult = await _reportRepo.GetAllDataByExpression(
                r => !r.IsDeleted,
                0, 0,
                r => r.CreatedAt,
                false, 
                r => r.User,
                r => r.Order
            );

            var reports = reportsResult.Items ?? new List<Report>();
            if (!reports.Any()) return new List<AdminReportDto>();

            var orderIds = reports
                .Where(r => r.OrderId != null)
                .Select(r => r.OrderId!)
                .Distinct()
                .ToList();

            var orderItemsResult = await _orderItemRepo.GetAllDataByExpression(
                oi => orderIds.Contains(oi.OrderId),
                0, 0,
                oi => oi.Id,
                true,
                oi => oi.Bike,
                oi => oi.Bike.Listing,
                oi => oi.Bike.Listing.User
            );

            var orderItems = orderItemsResult.Items ?? new List<OrderItem>();

            var result = reports.Select(r =>
            {
                var orderItem = orderItems.FirstOrDefault(oi => oi.OrderId == r.OrderId);
                var bike = orderItem?.Bike;

                return new AdminReportDto
                {
                    ReportId = r.Id,
                    ReportCode = $"#RP-{r.Id.ToString().Substring(0, 4).ToUpper()}",
                    CreatedAt = r.CreatedAt.ToString("dd/MM/yyyy HH:mm"),

                    ReporterName = r.User?.FullName ?? "N/A",
                    ReporterPhone = r.User?.PhoneNumber ?? "N/A",

                    BikeTitle = bike?.Listing?.Title ?? "Sản phẩm không tồn tại",
                    BikeCode = bike != null ? $"XE-{bike.Id.ToString().Substring(0, 5).ToUpper()}" : "N/A",
                    SellerName = bike?.Listing?.User?.FullName,

                    ReportType = r.Type.ToString(),
                    Reason = r.Reason,
                    Status = r.Status.ToString(),
                    OrderId = r.OrderId
                };
            }).ToList();

            return result;
        }

        public async Task<bool> ProgressReportStatusAsync(Guid reportId)
        {
            var report = await _reportRepo.GetById(reportId);
            if (report == null) throw new Exception("Không tìm thấy đơn tố cáo.");

            if (report.Status == ReportStatusEnum.Pending)
            {
                report.Status = ReportStatusEnum.Processing;
            }
            else if (report.Status == ReportStatusEnum.Processing)
            {
                report.Status = ReportStatusEnum.Resolved;
            }
            else
            {
                throw new Exception("Trạng thái hiện tại không thể chuyển tiếp (Đã xong hoặc đã bị từ chối).");
            }

            report.UpdatedAt = DateTimeHelper.NowVN();
            await _reportRepo.Update(report);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<bool> RejectReportAsync(Guid reportId)
        {
            var report = await _reportRepo.GetById(reportId);
            if (report == null) throw new Exception("Không tìm thấy đơn tố cáo.");

            if (report.Status == ReportStatusEnum.Resolved)
                throw new Exception("Báo cáo đã xử lý xong, không thể từ chối.");

            report.Status = ReportStatusEnum.Rejected;
            report.UpdatedAt = DateTimeHelper.NowVN();

            await _reportRepo.Update(report);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
    }
}
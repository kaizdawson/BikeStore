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

        public AdminService(IUnitOfWork unitOfWork, IGenericRepository<Listing> listingRepo, IGenericRepository<User> userRepo)
        {
            _unitOfWork = unitOfWork;
            _listingRepo = listingRepo;
            _userRepo = userRepo;
        }

        public async Task<bool> ApproveListingAsync(Guid id, AdminApproveDto dto)
        {
            var listing = await _listingRepo.GetById(id);
            if (listing == null) throw new Exception("Không tìm thấy tin đăng.");

            if (listing.Status != ListingStatusEnum.Draft)
                throw new Exception("Tin này không ở trạng thái chờ duyệt.");

            listing.Status = dto.IsApproved ? ListingStatusEnum.PendingApproval : ListingStatusEnum.Rejected;
            listing.UpdatedAt = DateTimeHelper.NowVN();

            await _listingRepo.Update(listing);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<List<object>> GetPendingListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Draft,
                pageNumber: 1,
                pageSize: 100,
                includes: new Expression<Func<Listing, object>>[]
                {
                    l => l.User,
                    l => l.Bikes
                }
            );
            //return result.Items.Select(l => new {
            //    l.Id,
            //    l.Title,
            //    SellerName = l.User?.FullName, 
            //    Price = l.Bikes.FirstOrDefault()?.Price ?? 0,
            //    Thumbnail = l.Bikes.FirstOrDefault()?.Medias.FirstOrDefault()?.Image,
            //    l.CreatedAt,
            //    l.Status
            //}).Cast<object>().ToList();

            return MapListingToResponse(result.Items);
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
                        b.Inspection.UserId,
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
                pageNumber: 1, pageSize: 100,
                includes: new Expression<Func<Listing, object>>[] {
            l => l.User,
            l => l.Bikes
                }
            );
            return MapListingToResponse(result.Items);
        }

        public async Task<List<object>> GetActiveListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Active && l.Bikes.Any(b => b.Status == BikeStatusEnum.Available),
                pageNumber: 1, pageSize: 100,
                includes: new Expression<Func<Listing, object>>[] {
            l => l.User,
            l => l.Bikes
                }
            );
            return MapListingToResponse(result.Items);
        }

        public async Task<List<object>> GetRejectedListingsAsync()
        {
            var result = await _listingRepo.GetAllDataByExpression(
                filter: l => l.Status == ListingStatusEnum.Rejected,
                pageNumber: 1, pageSize: 100,
                includes: new Expression<Func<Listing, object>>[] {
                    l => l.User,
                    l => l.Bikes
                }
            );
            return MapListingToResponse(result.Items);
        }

        private List<object> MapListingToResponse(IEnumerable<Listing> items)
        {
            return items.Select(l => {
                var firstBike = l.Bikes.FirstOrDefault();

                return (object)new
                {
                    l.Id,
                    l.Title,
                    SellerName = l.User?.FullName,
                    Price = firstBike?.Price ?? 0,
                    Thumbnail = firstBike?.Medias?.FirstOrDefault()?.Image,
                    InspectorName = firstBike?.Inspection?.User?.FullName ?? "Chưa có",
                    l.CreatedAt,
                    l.Status
                };
            }).ToList();
        }

        public async Task<(bool Success, string Message)> CreateInspectorAsync(SignUpDto dto)
        {
            // 1. Chuẩn hóa Email
            var email = dto.Email.Trim().ToLower();

            // 2. Kiểm tra Email/SĐT tồn tại (giống logic SignUp)
            var existed = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (existed != null)
                return (false, "Email này đã tồn tại trong hệ thống.");

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existedPhone = await _userRepo.GetFirstByExpression(u => u.PhoneNumber == dto.PhoneNumber);
                if (existedPhone != null)
                    return (false, "Số điện thoại này đã tồn tại.");
            }

            // 3. Khởi tạo Inspector (Gán Role = Inspector và Status = Active)
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
    }
}
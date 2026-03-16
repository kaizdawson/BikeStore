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
                pageNumber: 1, pageSize: 100,
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
                pageNumber: 1, pageSize: 100,
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
                pageNumber: 1, pageSize: 100,
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
    }
}
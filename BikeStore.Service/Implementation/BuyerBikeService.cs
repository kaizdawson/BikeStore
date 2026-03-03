using BikeStore.Common.Enums;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http; 
using System.Linq.Expressions;
using System.Security.Claims;   

namespace BikeStore.Service.Implementation
{
    public class BuyerBikeService : IBuyerBikeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IHttpContextAccessor _httpContextAccessor; 

        public BuyerBikeService(
            IUnitOfWork unitOfWork,
            IGenericRepository<Bike> bikeRepo,
            IHttpContextAccessor httpContextAccessor) 
        {
            _unitOfWork = unitOfWork;
            _bikeRepo = bikeRepo;
            _httpContextAccessor = httpContextAccessor;
        }

        private Guid? GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(userId) ? null : Guid.Parse(userId);
        }

        public async Task<List<object>> GetAllAvailableBikesAsync(int pageNumber, int pageSize)
        {
            var currentUserId = GetCurrentUserId();

            var result = await _bikeRepo.GetAllDataByExpression(
                filter: b => b.Status == BikeStatusEnum.Available,
                pageNumber: pageNumber,
                pageSize: pageSize,
                includes: new Expression<Func<Bike, object>>[] { b => b.Medias, b => b.Listing, b => b.Wishlists }
            );

            return result.Items.Select(b => (object)new
            {
                b.Id,
                Title = b.Listing?.Title ?? "Không có tiêu đề",
                b.Price,
                b.Brand,
                b.Category,
                Thumbnail = b.Medias.OrderBy(m => m.Id).Select(m => m.Image).FirstOrDefault() ?? "",
                b.Overall,
                IsWishlisted = currentUserId.HasValue && b.Wishlists.Any(w => w.UserId == currentUserId.Value),
                IsInspected = b.InspectionId != null
            }).ToList();
        }

        public async Task<object?> GetBikeDetailAsync(Guid id)
        {
            var currentUserId = GetCurrentUserId(); 

            var bike = await _bikeRepo.GetFirstByExpression(
                filter: b => b.Id == id,
                includeProperties: new Expression<Func<Bike, object>>[]
                {
                    b => b.Medias,
                    b => b.Listing,
                    b => b.Inspection,
                    b => b.Wishlists 
                }
            );

            if (bike == null) return null;

            return new
            {
                bike.Id,
                bike.Category,
                bike.Brand,
                bike.Price,
                bike.Status,
                bike.FrameSize,
                bike.FrameMaterial,
                bike.Paint,
                bike.Groupset,
                bike.Operating,
                bike.TireRim,
                bike.BrakeType,
                bike.Overall,
                bike.CreatedAt,
                bike.UpdatedAt,

                IsWishlisted = currentUserId.HasValue && bike.Wishlists.Any(w => w.UserId == currentUserId.Value),

                Listing = bike.Listing != null ? new
                {
                    bike.Listing.Id,
                    bike.Listing.Title,
                    bike.Listing.Description,
                    bike.Listing.Status,
                    bike.Listing.UserId,
                    bike.Listing.CreatedAt,
                    bike.Listing.UpdatedAt
                } : null,

                Inspection = bike.Inspection != null ? new
                {
                    bike.Inspection.Id,
                    bike.Inspection.UserId,
                    bike.Inspection.Frame,
                    bike.Inspection.PaintCondition,
                    bike.Inspection.Drivetrain,
                    bike.Inspection.Brakes,
                    bike.Inspection.Score,
                    bike.Inspection.Comment,
                    bike.Inspection.InspectionDate,
                    bike.Inspection.CreatedAt,
                    bike.Inspection.UpdatedAt
                } : null,

                Medias = bike.Medias.Select(m => new {
                    m.Id,
                    m.BikeId,
                    m.Image,
                    m.VideoUrl
                }).ToList()
            };
        }
    }
}
using BikeStore.Common.Enums;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http; 
using System.Linq.Expressions;
using System.Security.Claims;   

namespace BikeStore.Service.Implementation
{
    public class BuyerListingService : IBuyerListingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IHttpContextAccessor _httpContextAccessor; 

        public BuyerListingService(
            IUnitOfWork unitOfWork,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Listing> listingRepo,
            IHttpContextAccessor httpContextAccessor) 
        {
            _unitOfWork = unitOfWork;
            _bikeRepo = bikeRepo;
            _listingRepo = listingRepo;
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
                id=b.ListingId,
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

        public async Task<object?> GetListingDetailByListingIdAsync(Guid listingId)
        {
            var currentUserId = GetCurrentUserId();

            var listing = await _listingRepo.GetFirstByExpression(
                filter: l => l.Id == listingId,
                includeProperties: new Expression<Func<Listing, object>>[]
                {
            l => l.User,
            l => l.Bikes
                }
            );

            if (listing == null) return null;

            var bike = listing.Bikes.FirstOrDefault();
            if (bike == null) return null;

            var bikeDetail = await _bikeRepo.GetFirstByExpression(
                filter: b => b.Id == bike.Id,
                includeProperties: new Expression<Func<Bike, object>>[] {
            b => b.Medias,
            b => b.Inspection,
            b => b.Wishlists 
                }
            );

            if (bikeDetail == null) return null;

            return new
            {
                ListingId = listing.Id,
                listing.Title,
                listing.Description,
                listing.Status,
                listing.CreatedAt,
                SellerName = listing.User?.FullName,
                IsWishlisted = currentUserId.HasValue && bikeDetail.Wishlists.Any(w => w.UserId == currentUserId.Value),

                Bikes = new List<object>
                {
                    new {
                        bikeDetail.Id,
                        bikeDetail.Brand,
                        bikeDetail.Category,
                        bikeDetail.Price,
                        bikeDetail.FrameSize,
                        bikeDetail.FrameMaterial,
                        bikeDetail.Paint,
                        bikeDetail.Groupset,
                        bikeDetail.Operating,
                        bikeDetail.TireRim,
                        bikeDetail.BrakeType,
                        bikeDetail.Overall,
                        BikeStatus = bikeDetail.Status,

                        Inspections = bikeDetail.Inspection != null ? new List<object>
                        {
                            new {
                                bikeDetail.Inspection.Id,
                                bikeDetail.Inspection.Score,
                                bikeDetail.Inspection.Comment,
                                bikeDetail.Inspection.Frame,
                                bikeDetail.Inspection.PaintCondition,
                                bikeDetail.Inspection.Drivetrain,
                                bikeDetail.Inspection.Brakes,
                                bikeDetail.Inspection.InspectionDate
                            }
                        } : new List<object>(), 

                        Medias = bikeDetail.Medias.Select(m => new {
                            m.Id,
                            m.Image,
                            m.VideoUrl
                        }).ToList()
                        }
                 }
            };
        }
    }
}
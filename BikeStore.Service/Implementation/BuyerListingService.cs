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
        private readonly IGenericRepository<Review> _reviewRepo; 
        private readonly IGenericRepository<OrderItem> _orderItemRepo;

        public BuyerListingService(
            IUnitOfWork unitOfWork,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Listing> listingRepo,
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<Review> reviewRepo,
            IGenericRepository<OrderItem> orderItemRepo
            ) 
        {
            _unitOfWork = unitOfWork;
            _bikeRepo = bikeRepo;
            _listingRepo = listingRepo;
            _httpContextAccessor = httpContextAccessor;
            _reviewRepo = reviewRepo;
            _orderItemRepo = orderItemRepo;
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

            return MapBikesToResponse(result.Items, GetCurrentUserId());
        }

            public async Task<object?> GetListingDetailByListingIdAsync(Guid listingId)
            {
                var currentUserId = GetCurrentUserId();

                var listing = await _listingRepo.GetFirstByExpression(
                    filter: l => l.Id == listingId,
                    includeProperties: new Expression<Func<Listing, object>>[] { l => l.User, l => l.Bikes }
                );

                if (listing == null || listing.User == null) return null;
                var seller = listing.User;
                string joinDateString = $"Tham gia từ {seller.CreatedAt.ToString("dd/MM/yyyy")}";

                var bike = listing.Bikes.FirstOrDefault();
                if (bike == null) return null;
                var bikeDetail = await _bikeRepo.GetFirstByExpression(
                    filter: b => b.Id == bike.Id,
                    includeProperties: new Expression<Func<Bike, object>>[] { b => b.Medias, b => b.Inspection, b => b.Wishlists }
                );



                var sellerOrderItemsResult = await _orderItemRepo.GetAllDataByExpression(
                    filter: oi => oi.Bike.Listing.UserId == seller.Id,
                    pageNumber: 0, pageSize: 0,
                    orderBy: oi => oi.Id, isAscending: true,
                    includes: new Expression<Func<OrderItem, object>>[] { oi => oi.Bike.Listing }
                );

                var sellerOrderIds = sellerOrderItemsResult.Items?
                    .Select(oi => oi.OrderId)
                    .Distinct()
                    .ToList() ?? new List<Guid>();

                var reviewsResult = await _reviewRepo.GetAllDataByExpression(
                    filter: r => sellerOrderIds.Contains(r.OrderId),
                    pageNumber: 0, pageSize: 0,
                    orderBy: r => r.CreatedAt, isAscending: false,
                    includes: new Expression<Func<Review, object>>[] {
                r => r.Order,
                r => r.Order.User
                    }
                );

                var reviews = reviewsResult.Items ?? new List<Review>();

                var totalReviews = reviews.Count;
                var avgRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;

                var latestReviews = reviews.Take(3).Select(r => new {
                    ReviewerName = r.Order?.User?.FullName ?? "Người dùng ẩn danh",
                    r.Rating,
                    r.Comment,
                    Date = r.CreatedAt.ToString("dd/MM/yyyy")
                }).ToList();

                return new
                {
                    ListingId = listing.Id,
                    listing.Title,
                    listing.Description,
                    listing.Status,
                    listing.CreatedAt,
                    SellerName = seller.FullName,
                    IsWishlisted = currentUserId.HasValue && bikeDetail.Wishlists.Any(w => w.UserId == currentUserId.Value),

                    SellerReviewSummary = new
                    {
                        SellerName = seller.FullName,
                        JoinDate = joinDateString,
                        AverageRating = Math.Round(avgRating, 1),
                        TotalReviews = totalReviews,
                        LatestReviews = latestReviews
                    },

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
        public async Task<List<object>> SearchBikesByNameAsync(string name, int pageNumber, int pageSize)
        {
            var searchName = name?.Trim().ToLower();

            var result = await _bikeRepo.GetAllDataByExpression(
                filter: b => b.Status == BikeStatusEnum.Available &&
                             (string.IsNullOrEmpty(searchName) || (b.Listing != null && b.Listing.Title.ToLower().Contains(searchName))),
                pageNumber: pageNumber,
                pageSize: pageSize,
                includes: new Expression<Func<Bike, object>>[] { b => b.Medias, b => b.Listing, b => b.Wishlists }
            );

            return MapBikesToResponse(result.Items, GetCurrentUserId());
        }

        public async Task<List<object>> FilterBikesByTagsAsync(List<string> tags, int pageNumber, int pageSize)
        {
            var normalizedTags = (tags ?? new List<string>())
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToLower())
                .ToList();

            var result = await _bikeRepo.GetAllDataByExpression(
                filter: b => b.Status == BikeStatusEnum.Available &&
                             (normalizedTags.Count == 0 ||
                              normalizedTags.Contains(b.Brand.ToLower()) ||
                              normalizedTags.Contains(b.Category.ToLower())),
                pageNumber: pageNumber,
                pageSize: pageSize,
                includes: new Expression<Func<Bike, object>>[] { b => b.Medias, b => b.Listing, b => b.Wishlists }
            );

            return MapBikesToResponse(result.Items, GetCurrentUserId());
        }

        private List<object> MapBikesToResponse(IEnumerable<Bike> bikes, Guid? currentUserId)
        {
            return bikes.Select(b => (object)new
            {
                id = b.ListingId,
                Title = b.Listing?.Title ?? "Không có tiêu đề",
                BikeId = b.Id,
                b.Price,
                b.Brand,
                b.Category,
                Thumbnail = b.Medias.OrderBy(m => m.Id).Select(m => m.Image).FirstOrDefault() ?? "",
                b.Overall,
                IsWishlisted = currentUserId.HasValue && b.Wishlists.Any(w => w.UserId == currentUserId.Value),
                IsInspected = b.InspectionId != null
            }).ToList();
        }
    }
}
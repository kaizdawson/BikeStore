using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System.Linq.Expressions;

namespace BikeStore.Service.Implementation
{
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Wishlist> _wishlistRepo;
        private readonly IGenericRepository<Bike> _bikeRepo; 

        public WishlistService(IUnitOfWork unitOfWork,
                               IGenericRepository<Wishlist> wishlistRepo,
                               IGenericRepository<Bike> bikeRepo)
        {
            _unitOfWork = unitOfWork;
            _wishlistRepo = wishlistRepo;
            _bikeRepo = bikeRepo;
        }

        public async Task<List<WishlistDto>> GetMyWishlistAsync(Guid userId)
        {
            var result = await _wishlistRepo.GetAllDataByExpression(
                filter: w => w.UserId == userId,
                pageNumber: 0, pageSize: 0,
                includes: new Expression<Func<Wishlist, object>>[]
                {
                w => w.Bike,
                w => w.Bike.Listing,
                w => w.Bike.Medias
                }
            );

            return result.Items
                .Where(w => w.Bike != null && w.Bike.Listing != null)
                .Select(w => new WishlistDto
                {
                    Id = w.Id,
                    ListingId = w.Bike.ListingId,
                    BikeId = w.BikeId,
                    Title = w.Bike.Listing.Title,
                    Price = w.Bike.Price,
                    Brand = w.Bike.Brand,
                    Category = w.Bike.Category,
                    BikeStatus = w.Bike.Status.ToString(),
                    ImageUrl = w.Bike.Medias?
                        .Where(m => !string.IsNullOrEmpty(m.Image))
                        .OrderBy(m => m.Id)
                        .Select(m => m.Image)
                        .FirstOrDefault() ?? "default-image-url.png"
                }).ToList();
            }

        public async Task<bool> AddToWishlistAsync(Guid userId, Guid bikeId)
        {
            var bike = await _bikeRepo.GetById(bikeId);
            if (bike == null)
                throw new Exception("Xe không tồn tại.");

            var existing = await _wishlistRepo.GetFirstByExpression(w => w.UserId == userId && w.BikeId == bikeId);
            if (existing != null)
            {
                throw new Exception("Xe này đã có trong danh sách yêu thích của bạn.");
            }
            if (bike.Status != BikeStatusEnum.Available)
            {
                string statusName = bike.Status.ToString();
                throw new Exception($"Không thể thêm vào giỏ hàng vì xe hiện đang ở trạng thái: {statusName}");
            }
            var newItem = new Wishlist
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BikeId = bikeId
            };

            await _wishlistRepo.Insert(newItem);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid bikeId)
        {
            var item = await _wishlistRepo.GetFirstByExpression(w => w.UserId == userId && w.BikeId == bikeId);

            if (item == null) return false;

            await _wishlistRepo.Delete(item);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
    }
}
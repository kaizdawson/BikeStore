using BikeStore.Common.DTOs.Buyer;
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
        private readonly IGenericRepository<Bike> _bikeRepo; // Thêm repo xe để check

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

            // Chuyển đổi an toàn: lọc bỏ những wishlist có xe đã bị xóa khỏi hệ thống (nếu có)
            return result.Items
                .Where(w => w.Bike != null && w.Bike.Listing != null)
                .Select(w => new WishlistDto
                {
                    Id = w.Id,
                    BikeId = w.BikeId,
                    Title = w.Bike.Listing.Title,
                    Price = w.Bike.Price,
                    ImageUrl = w.Bike.Medias?.FirstOrDefault()?.Image ?? "default-image-url.png"
                }).ToList();
        }

        public async Task<bool> AddToWishlistAsync(Guid userId, Guid bikeId)
        {
            // 1. Kiểm tra xe có tồn tại và có đang Active/Available không
            var bike = await _bikeRepo.GetById(bikeId);
            if (bike == null)
                throw new Exception("Xe không tồn tại.");

            // 2. Kiểm tra xem xe này đã nằm trong Wishlist của User chưa
            var existing = await _wishlistRepo.GetFirstByExpression(w => w.UserId == userId && w.BikeId == bikeId);
            if (existing != null)
            {
                // Thay vì return true, ta ném lỗi để Controller bắt được và hiện thông báo
                throw new Exception("Xe này đã có trong danh sách yêu thích của bạn.");
            }

            // 3. Tạo mới bản ghi
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
            // Tìm bản ghi chính xác của User đó và Xe đó
            var item = await _wishlistRepo.GetFirstByExpression(w => w.UserId == userId && w.BikeId == bikeId);

            if (item == null) return false;

            await _wishlistRepo.Delete(item);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
    }
}
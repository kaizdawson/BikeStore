using BikeStore.Common.DTOs;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq.Expressions;

namespace BikeStore.Service.Implementation
{
    public class CartItemService : ICartItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IGenericRepository<CartItem> _itemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;

        public CartItemService(IUnitOfWork unitOfWork,
            IGenericRepository<Cart> cartRepo,
            IGenericRepository<CartItem> itemRepo,
            IGenericRepository<Bike> bikeRepo)
        {
            _unitOfWork = unitOfWork;
            _cartRepo = cartRepo;
            _itemRepo = itemRepo;
            _bikeRepo = bikeRepo;
        }

        public async Task<List<CartItemDto>> GetItemsByCartIdAsync(Guid cartId)
        {
            // GetAllDataByExpression trả về PagedResult<T> nên cần truy cập vào .Items
            var result = await _itemRepo.GetAllDataByExpression(
                filter: i => i.CartId == cartId,
                pageNumber: 0, 
                pageSize: 100,
                includes: new Expression<Func<CartItem, object>>[] {
                    i => i.Bike.Listing,
                    i => i.Bike.Medias
                }
            );

            return result.Items.Select(i => new CartItemDto
            {
                Id = i.Id,
                BikeId = i.BikeId,
                BikeTitle = i.Bike.Listing.Title, 
                UnitPrice = i.UnitPrice,
                ImageUrl = i.Bike.Medias.FirstOrDefault()?.Image, 
                IsSelected = i.IsSelected
            }).ToList();
        }

        public async Task<bool> AddItemAsync(Guid userId, Guid bikeId)
        {
            var bike = await _bikeRepo.GetById(bikeId);
            if (bike == null) throw new Exception("Xe không tồn tại trong hệ thống.");

            var cart = await _cartRepo.GetFirstByExpression(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTimeHelper.NowVN() 
                };
                await _cartRepo.Insert(cart);
                await _unitOfWork.SaveChangeAsync();
            }

            var existingItem = await _itemRepo.GetFirstByExpression(
                i => i.CartId == cart.Id && i.BikeId == bikeId
            );

            if (existingItem != null)
            {
                throw new Exception("Xe này đã có trong giỏ hàng của bạn rồi.");
            }

            var newItem = new CartItem
            {
                Id = Guid.NewGuid(),
                CartId = cart.Id,
                BikeId = bikeId,
                UnitPrice = bike.Price, 
                IsSelected = true
            };

            await _itemRepo.Insert(newItem);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<bool> RemoveItemAsync(Guid cartItemId)
        {
            var item = await _itemRepo.GetById(cartItemId);
            if (item == null) return false;

            await _itemRepo.Delete(item);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
        public async Task<bool> ToggleSelectionAsync(Guid cartItemId)
        {
            // 1. Lấy CartItem kèm theo thông tin của Bike để check Status
            var item = await _itemRepo.GetFirstByExpression(
                filter: i => i.Id == cartItemId,
                includeProperties: new Expression<Func<CartItem, object>>[] { i => i.Bike }
            );

            if (item == null) throw new Exception("Không tìm thấy món hàng.");

            // 2. Nếu người dùng muốn CHỌN (chuyển sang true) nhưng xe không còn Available
            // Giả sử Status 2 là Available (BikeStatusEnum.Available)
            if (!item.IsSelected && item.Bike.Status != BikeStatusEnum.Available)
            {
                throw new Exception($"Không thể chọn sản phẩm này vì xe hiện đang {item.Bike.Status} (không còn sẵn sàng).");
            }

            // 3. Thực hiện đảo ngược trạng thái nếu hợp lệ
            item.IsSelected = !item.IsSelected;

            await _itemRepo.Update(item);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
        public async Task<List<string>> ValidateCartAsync(Guid cartId)
        {
            var warnings = new List<string>();

            var result = await _itemRepo.GetAllDataByExpression(
                filter: i => i.CartId == cartId,
                pageNumber: 1,           
                pageSize: 100,           
                orderBy: null,           
                isAscending: true,       
                includes: new Expression<Func<CartItem, object>>[] { i => i.Bike.Listing }
            );

            foreach (var item in result.Items)
            {
                if (item.Bike.Status != BikeStatusEnum.Available)
                {
                    warnings.Add($"Xe '{item.Bike.Listing.Title}' hiện không còn khả dụng.");

                    if (item.IsSelected)
                    {
                        item.IsSelected = false; 
                        await _itemRepo.Update(item);
                    }
                }
            }

            if (warnings.Any()) await _unitOfWork.SaveChangeAsync();
            return warnings;
        }
    }
}
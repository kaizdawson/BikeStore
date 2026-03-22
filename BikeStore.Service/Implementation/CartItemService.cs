using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Linq.Expressions;
using System.Security.Claims;

namespace BikeStore.Service.Implementation
{
    public class CartItemService : ICartItemService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IGenericRepository<CartItem> _itemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public CartItemService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<Cart> cartRepo,
            IGenericRepository<CartItem> itemRepo,
            IGenericRepository<Bike> bikeRepo)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _cartRepo = cartRepo;
            _itemRepo = itemRepo;
            _bikeRepo = bikeRepo;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId == null ? Guid.Empty : Guid.Parse(userId);
        }
        public async Task<List<CartItemDto>> GetCartItemsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null) throw new UnauthorizedAccessException();

            var cart = await _cartRepo.GetFirstByExpression(c => c.UserId == userId);

            if (cart == null) return new List<CartItemDto>();

            var result = await _itemRepo.GetAllDataByExpression(
                filter: i => i.CartId == cart.Id,
                pageNumber: 1, 
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
                UnitPrice = i.Bike.Price,
                ImageUrl = i.Bike.Medias.FirstOrDefault()?.Image, 
                IsSelected = i.IsSelected,
                BikeStatus = i.Bike.Status.ToString()
            }).ToList();
        }

        public async Task<bool> AddItemAsync(Guid userId, Guid bikeId)
        {
            var bike = await _bikeRepo.GetById(bikeId);
            if (bike == null) throw new Exception("Xe không tồn tại trong hệ thống.");
            if (bike.Status != BikeStatusEnum.Available)
            {
                string statusName = bike.Status.ToString(); 
                throw new Exception($"Không thể thêm vào giỏ hàng vì xe hiện đang ở trạng thái: {statusName}");
            }

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
            var item = await _itemRepo.GetFirstByExpression(
                filter: i => i.Id == cartItemId,
                includeProperties: new Expression<Func<CartItem, object>>[] { i => i.Bike }
            );

            if (item == null) throw new Exception("Không tìm thấy món hàng.");

            if (!item.IsSelected && item.Bike.Status != BikeStatusEnum.Available)
            {
                throw new Exception($"Không thể chọn sản phẩm này vì xe hiện đang {item.Bike.Status} (không còn sẵn sàng).");
            }

            item.IsSelected = !item.IsSelected;

            await _itemRepo.Update(item);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }
        public async Task<List<string>> ValidateCartAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) throw new UnauthorizedAccessException("Người dùng chưa đăng nhập.");
            var cart = await _cartRepo.GetFirstByExpression(c => c.UserId == userId);
            if (cart == null) return new List<string>();
            var warnings = new List<string>();

            var result = await _itemRepo.GetAllDataByExpression(
                filter: i => i.CartId == cart.Id,
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
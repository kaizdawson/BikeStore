using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System.Linq.Expressions;

namespace BikeStore.Service.Implementation
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IGenericRepository<CartItem> _itemRepo;


        public CartService(IUnitOfWork unitOfWork, IGenericRepository<Cart> cartRepo, IGenericRepository<CartItem> itemRepo)
        {
            _unitOfWork = unitOfWork;
            _cartRepo = cartRepo;
            _itemRepo = itemRepo;
        }

        public async Task<CartDto> GetMyCartAsync(Guid userId)
        {
            var cart = await _cartRepo.GetFirstByExpression(
                filter: c => c.UserId == userId,
                includeProperties: new Expression<Func<Cart, object>>[] { c => c.CartItems}
            );

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    CreatedAt = DateTimeHelper.NowVN(),
                    CartItems = new List<CartItem>()
                };
                await _cartRepo.Insert(cart);
                await _unitOfWork.SaveChangeAsync();
            }

            var result = await _itemRepo.GetAllDataByExpression(
                    filter: i => i.CartId == cart.Id,
                    pageNumber: 0,
                    pageSize: 0,
                    includes: new Expression<Func<CartItem, object>>[] { i => i.Bike }
                );

            var allItems = result.Items ?? new List<CartItem>();
            var selectedItems = allItems.Where(x => x.IsSelected).ToList();

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                TotalAmount = selectedItems.Sum(x => x.Bike?.Price ?? 0),
                SelectedItemCount = selectedItems.Count
            };
        }
    }
}
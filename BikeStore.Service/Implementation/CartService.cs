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

        public CartService(IUnitOfWork unitOfWork, IGenericRepository<Cart> cartRepo)
        {
            _unitOfWork = unitOfWork;
            _cartRepo = cartRepo;
        }

        public async Task<CartDto> GetMyCartAsync(Guid userId)
        {
            var cart = await _cartRepo.GetFirstByExpression(
                filter: c => c.UserId == userId,
                includeProperties: new Expression<Func<Cart, object>>[] { c => c.CartItems }
            );

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

            return new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                TotalAmount = cart.CartItems.Where(x => x.IsSelected).Sum(x => x.UnitPrice)
            };
        }
    }
}
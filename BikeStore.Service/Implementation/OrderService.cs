using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using System.Linq.Expressions;
using System.Security.Claims;

namespace BikeStore.Service.Implementation
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<Cart> _cartRepo;
        private readonly IGenericRepository<CartItem> _itemRepo;
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;

        public OrderService(
            IUnitOfWork unitOfWork,
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<Cart> cartRepo,
            IGenericRepository<CartItem> itemRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<Bike> bikeRepo)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _cartRepo = cartRepo;
            _itemRepo = itemRepo;
            _orderRepo = orderRepo;
            _bikeRepo = bikeRepo;
        }

        private Guid GetCurrentUserId()
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return userId == null ? Guid.Empty : Guid.Parse(userId);
        }

        public async Task<Guid> CreateOrderAsync(OrderDto dto)
        {
            var userId = GetCurrentUserId();

            var cart = await _cartRepo.GetFirstByExpression(c => c.UserId == userId);
            if (cart == null) throw new Exception("Giỏ hàng trống.");

            var selectedItems = await _itemRepo.GetAllDataByExpression(
                filter: i => i.CartId == cart.Id && i.IsSelected == true,
                pageNumber: 1,
                pageSize: 100,
                includes: new Expression<Func<CartItem, object>>[] { i => i.Bike }
            );

            if (!selectedItems.Items.Any()) throw new Exception("Vui lòng chọn sản phẩm trong giỏ hàng.");

            foreach (var item in selectedItems.Items)
            {
                if (item.Bike.Status != BikeStatusEnum.Available)
                    throw new Exception($"Xe '{item.Bike.Brand}' đã có người khác đặt mua.");
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = OrderStatusEnum.Pending,
                ReceiverName = dto.ReceiverName,
                ReceiverPhone = dto.ReceiverPhone,
                ReceiverAddress = dto.ReceiverAddress,
                TotalAmount = selectedItems.Items.Sum(i => i.UnitPrice),
                CreatedAt = DateTimeHelper.NowVN()
            };

            foreach (var item in selectedItems.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    BikeId = item.BikeId,
                    UnitPrice = item.UnitPrice
                });

                await _itemRepo.Delete(item);
            }

            await _orderRepo.Insert(order);
            await _unitOfWork.SaveChangeAsync();

            return order.Id;
        }

        public async Task<List<object>> GetMyOrdersAsync()
        {
            var userId = GetCurrentUserId();
            var result = await _orderRepo.GetAllDataByExpression(
                filter: o => o.UserId == userId,
                pageNumber: 1,
                pageSize: 50,
                orderBy: o => o.CreatedAt,
                isAscending: false,
                includes: new Expression<Func<Order, object>>[] { o => o.OrderItems }
            );

            var listOrder = new List<object>();

            foreach (var o in result.Items)
            {
                var firstItem = o.OrderItems.FirstOrDefault();
                var img = "";

                if (firstItem != null)
                {
                    var bike = await _bikeRepo.GetFirstByExpression(
                        filter: b => b.Id == firstItem.BikeId,
                        includeProperties: new Expression<Func<Bike, object>>[] { b => b.Medias }
                    );

                    img = bike?.Medias?
                        .Where(m => !string.IsNullOrEmpty(m.Image))
                        .OrderBy(m => m.Id)
                        .Select(m => m.Image)
                        .FirstOrDefault() ?? "";
                }

                listOrder.Add(new
                {
                    o.Id,
                    o.CreatedAt,
                    Status = o.Status.ToString(),
                    o.TotalAmount,
                    o.ReceiverName,
                    TotalItems = o.OrderItems.Count,
                    Thumbnail = img
                });
            }

            return listOrder;
        }

        public async Task<object?> GetOrderDetailAsync(Guid orderId)
        {
            var userId = GetCurrentUserId();

            var order = await _orderRepo.GetFirstByExpression(
                filter: o => o.Id == orderId && o.UserId == userId,
                includeProperties: new Expression<Func<Order, object>>[] {
            o => o.OrderItems,
            o => o.Transaction!
                }
            );

            if (order == null) return null;

            var detailedItems = new List<object>();

            foreach (var item in order.OrderItems)
            {
                var bike = await _bikeRepo.GetFirstByExpression(
                    filter: b => b.Id == item.BikeId,
                    includeProperties: new Expression<Func<Bike, object>>[] {
                b => b.Listing!,
                b => b.Medias
                    }
                );

                var thumbnail = bike?.Medias?
                    .Where(m => !string.IsNullOrEmpty(m.Image)) 
                    .OrderBy(m => m.Id)
                    .Select(m => m.Image)
                    .FirstOrDefault() ?? "";

                detailedItems.Add(new
                {
                    item.Id,
                    item.BikeId,
                    item.UnitPrice,
                    Title = bike?.Listing?.Title ?? "Không có tiêu đề",
                    bike?.Brand,
                    bike?.Category,
                    Thumbnail = thumbnail 
                });
            }

            return new
            {
                order.Id,
                Status = order.Status.ToString(),
                order.TotalAmount,
                order.ReceiverName,
                order.ReceiverPhone,
                order.ReceiverAddress,
                order.CreatedAt,
                Transaction = order.Transaction != null ? new
                {
                    order.Transaction.OrderCode,
                    order.Transaction.Amount,
                    order.Transaction.Status,
                    order.Transaction.Description,
                    order.Transaction.PaidAt
                } : null,
                OrderItems = detailedItems
            };
        }

        public async Task<bool> CancelOrderAsync(Guid orderId)
        {
            var userId = GetCurrentUserId();
            var order = await _orderRepo.GetFirstByExpression(
                filter: o => o.Id == orderId && o.UserId == userId,
                includeProperties: new Expression<Func<Order, object>>[] { o => o.OrderItems }
            );

            if (order == null) throw new Exception("Đơn hàng không tồn tại.");
            if (order.Status != OrderStatusEnum.Pending)
                throw new Exception("Chỉ có thể hủy đơn hàng đang ở trạng thái chờ.");

            order.Status = OrderStatusEnum.Cancelled;

            foreach (var item in order.OrderItems)
            {
                var bike = await _bikeRepo.GetById(item.BikeId);
                if (bike != null)
                {
                    bike.Status = BikeStatusEnum.Available;
                    await _bikeRepo.Update(bike);
                }
            }

            await _orderRepo.Update(order);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<bool> UpdateStatusAsync(Guid orderId, OrderStatusEnum newStatus)
        {
            var order = await _orderRepo.GetById(orderId);
            if (order == null) throw new Exception("Không tìm thấy đơn hàng.");

            order.Status = newStatus;
            order.UpdatedAt = DateTimeHelper.NowVN();

            await _orderRepo.Update(order);
            return await _unitOfWork.SaveChangeAsync() > 0;
        }

        public async Task<List<object>> GetAllOrdersAsync()
        {
            var result = await _orderRepo.GetAllDataByExpression(
                filter: null,
                pageNumber: 1,
                pageSize: 100,
                orderBy: o => o.CreatedAt,
                isAscending: false,
                includes: new Expression<Func<Order, object>>[] { o => o.OrderItems }
            );

            return result.Items.Select(o => (object)new
            {
                o.Id,
                o.CreatedAt,
                Status = o.Status.ToString(),
                o.TotalAmount,
                o.ReceiverName,
                TotalItems = o.OrderItems.Count
            }).ToList();
        }

        public async Task<Guid> BuyNowAsync(Guid bikeId)
        {
            var userId = GetCurrentUserId();
            if (userId == Guid.Empty) throw new Exception("Vui lòng đăng nhập.");

            var bike = await _bikeRepo.GetFirstByExpression(
                filter: b => b.Id == bikeId && !b.IsDeleted
            );

            if (bike == null) throw new Exception("Sản phẩm không tồn tại.");
            if (bike.Status != BikeStatusEnum.Available)
                throw new Exception("Sản phẩm này hiện không còn khả dụng.");

            var cart = await _cartRepo.GetFirstByExpression(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart { Id = Guid.NewGuid(), UserId = userId };
                await _cartRepo.Insert(cart);
                await _unitOfWork.SaveChangeAsync();
            }

            var allItemsInCart = await _itemRepo.GetListByExpression(i => i.CartId == cart.Id);

            foreach (var item in allItemsInCart)
            {
                item.IsSelected = false;
            }
            await _itemRepo.UpdateRange(allItemsInCart);

            var existingItem = allItemsInCart.FirstOrDefault(i => i.BikeId == bikeId);
            if (existingItem != null)
            {
                existingItem.IsSelected = true;
                await _itemRepo.Update(existingItem);
            }
            else
            {
                var newItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    BikeId = bikeId,
                    UnitPrice = bike.Price,
                    IsSelected = true
                };
                await _itemRepo.Insert(newItem);
            }

            await _unitOfWork.SaveChangeAsync();

            return cart.Id;
        }
    }
}
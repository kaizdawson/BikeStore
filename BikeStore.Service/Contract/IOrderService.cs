using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IOrderService
    {
        Task<Guid> CreateOrderAsync( OrderDto dto);
        Task<List<object>> GetMyOrdersAsync();
        Task<object?> GetOrderDetailAsync(Guid orderId);
        Task<List<object>> GetAllOrdersAsync();
        Task<bool> CancelOrderAsync(Guid orderId); 
        Task<bool> UpdateStatusAsync(Guid orderId, OrderStatusEnum newStatus);

        Task<Guid> BuyNowAsync(Guid bikeId, OrderDto dto);
    }
}

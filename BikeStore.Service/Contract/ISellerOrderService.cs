using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Order;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ISellerOrderService
    {
        Task<PagedResult<SellerOrderDto>> GetPaidOrdersAsync(Guid sellerId, int pageNumber, int pageSize);

        Task<SellerOrderDto?> ConfirmOrderAsync(Guid sellerId, Guid orderId);
        Task<SellerOrderDto?> ShipOrderAsync(Guid sellerId, Guid orderId);
        Task<SellerOrderDto?> CompleteOrderAsync(Guid sellerId, Guid orderId);

        Task<SellerOrderItemDetailDto?> GetOrderItemDetailsAsync(Guid sellerId, Guid orderItemId);
    }
}

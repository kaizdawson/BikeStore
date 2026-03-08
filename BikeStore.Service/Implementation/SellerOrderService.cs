using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Order;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Implementation
{
    public class SellerOrderService : ISellerOrderService
    {
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Media> _mediaRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IUnitOfWork _uow;

        public SellerOrderService(
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Media> mediaRepo,
            IGenericRepository<Listing> listingRepo,
            IUnitOfWork uow)
        {
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _bikeRepo = bikeRepo;
            _mediaRepo = mediaRepo;
            _listingRepo = listingRepo;
            _uow = uow;
        }

        public async Task<PagedResult<SellerOrderDto>> GetPaidOrdersAsync(Guid sellerId, int pageNumber, int pageSize)
        {
            var orderRes = await _orderRepo.GetAllDataByExpression(
                filter: o => o.Status == OrderStatusEnum.Paid,
                pageNumber: pageNumber,
                pageSize: pageSize,
                orderBy: o => o.CreatedAt,
                isAscending: false
            );

            var orders = orderRes.Items?.ToList() ?? new List<Order>();
            if (!orders.Any())
            {
                return new PagedResult<SellerOrderDto>
                {
                    TotalPages = orderRes.TotalPages,
                    Items = new List<SellerOrderDto>()
                };
            }

            var orderIds = orders.Select(o => o.Id).ToList();

            var orderItemsRes = await _orderItemRepo.GetAllDataByExpression(
                filter: oi => orderIds.Contains(oi.OrderId),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: oi => oi.Id,
                isAscending: true
            );

            var orderItems = orderItemsRes.Items?.ToList() ?? new List<OrderItem>();
            if (!orderItems.Any())
            {
                return new PagedResult<SellerOrderDto>
                {
                    TotalPages = orderRes.TotalPages,
                    Items = new List<SellerOrderDto>()
                };
            }

            var bikeIds = orderItems.Select(x => x.BikeId).Distinct().ToList();

            var bikesRes = await _bikeRepo.GetAllDataByExpression(
                filter: b => bikeIds.Contains(b.Id),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: b => b.CreatedAt,
                isAscending: false
            );

            var bikes = bikesRes.Items?.ToList() ?? new List<Bike>();

            var listingIds = bikes.Select(b => b.ListingId).Distinct().ToList();

            var listingRes = await _listingRepo.GetAllDataByExpression(
                filter: l => listingIds.Contains(l.Id) && l.UserId == sellerId,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: l => l.CreatedAt,
                isAscending: false
            );

            var ownedListingIds = listingRes.Items?
                .Select(l => l.Id)
                .ToHashSet() ?? new HashSet<Guid>();

            var sellerBikeMap = bikes
                .Where(b => ownedListingIds.Contains(b.ListingId))
                .ToDictionary(b => b.Id, b => b);

            if (!sellerBikeMap.Any())
            {
                return new PagedResult<SellerOrderDto>
                {
                    TotalPages = orderRes.TotalPages,
                    Items = new List<SellerOrderDto>()
                };
            }

            var sellerBikeIds = sellerBikeMap.Keys.ToList();

            var mediaRes = await _mediaRepo.GetAllDataByExpression(
                filter: m => sellerBikeIds.Contains(m.BikeId),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: m => m.Id,
                isAscending: true
            );

            var medias = mediaRes.Items?.ToList() ?? new List<Media>();

            var imageMap = medias
                .GroupBy(m => m.BikeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Image).FirstOrDefault(img => !string.IsNullOrWhiteSpace(img))
                );

            var groupedItems = orderItems
                .Where(oi => sellerBikeIds.Contains(oi.BikeId))
                .GroupBy(oi => oi.OrderId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var resultItems = orders
                .Where(o => groupedItems.ContainsKey(o.Id))
                .Select(o =>
                {
                    var dto = new SellerOrderDto
                    {
                        Id = o.Id,
                        Status = o.Status.ToString(),
                        ReceiverName = o.ReceiverName,
                        ReceiverPhone = o.ReceiverPhone,
                        ReceiverAddress = o.ReceiverAddress,
                        TotalAmount = o.TotalAmount,
                        CreatedAt = DateTimeHelper.ToVN(o.CreatedAt),
                        UpdatedAt = o.UpdatedAt.HasValue
    ? DateTimeHelper.ToVN(o.UpdatedAt.Value)
    : null,
                        Items = groupedItems[o.Id].Select(oi =>
                        {
                            sellerBikeMap.TryGetValue(oi.BikeId, out var bike);
                            imageMap.TryGetValue(oi.BikeId, out var image);

                            return new SellerOrderItemDto
                            {
                                Id = oi.Id,
                                BikeId = oi.BikeId,
                                UnitPrice = oi.UnitPrice,
                                LineTotal = oi.LineTotal,
                                BikeBrand = bike?.Brand,
                                BikeCategory = bike?.Category,
                                Image = image
                            };
                        }).ToList()
                    };

                    return dto;
                }).ToList();

            return new PagedResult<SellerOrderDto>
            {
                TotalPages = orderRes.TotalPages,
                Items = resultItems
            };
        }

        public async Task<SellerOrderDto?> ConfirmOrderAsync(Guid sellerId, Guid orderId)
        {
            var order = await GetOwnedOrderByStatusAsync(sellerId, orderId, OrderStatusEnum.Paid);
            if (order == null) return null;

            order.Status = OrderStatusEnum.Confirmed;
            order.UpdatedAt = DateTimeHelper.NowVN();

            await _orderRepo.Update(order);
            await _uow.SaveChangeAsync();

            return await BuildOrderDtoAsync(sellerId, order);
        }

        public async Task<SellerOrderDto?> ShipOrderAsync(Guid sellerId, Guid orderId)
        {
            var order = await GetOwnedOrderByStatusAsync(sellerId, orderId, OrderStatusEnum.Confirmed);
            if (order == null) return null;

            order.Status = OrderStatusEnum.Shipping;
            order.UpdatedAt = DateTimeHelper.NowVN();

            await _orderRepo.Update(order);
            await _uow.SaveChangeAsync();

            return await BuildOrderDtoAsync(sellerId, order);
        }

        public async Task<SellerOrderDto?> CompleteOrderAsync(Guid sellerId, Guid orderId)
        {
            var order = await GetOwnedOrderByStatusAsync(sellerId, orderId, OrderStatusEnum.Shipping);
            if (order == null) return null;

            order.Status = OrderStatusEnum.Completed;
            order.UpdatedAt = DateTimeHelper.NowVN();

            await _orderRepo.Update(order);
            await _uow.SaveChangeAsync();

            return await BuildOrderDtoAsync(sellerId, order);
        }

        private async Task<Order?> GetOwnedOrderByStatusAsync(Guid sellerId, Guid orderId, OrderStatusEnum requiredStatus)
        {
            var order = await _orderRepo.GetFirstByExpression(x => x.Id == orderId);
            if (order == null) return null;
            if (order.Status != requiredStatus) return null;

            var orderItemsRes = await _orderItemRepo.GetAllDataByExpression(
                filter: oi => oi.OrderId == orderId,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: oi => oi.Id,
                isAscending: true
            );

            var orderItems = orderItemsRes.Items?.ToList() ?? new List<OrderItem>();
            if (!orderItems.Any()) return null;

            var bikeIds = orderItems.Select(oi => oi.BikeId).Distinct().ToList();

            var bikesRes = await _bikeRepo.GetAllDataByExpression(
                filter: b => bikeIds.Contains(b.Id),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: b => b.CreatedAt,
                isAscending: false
            );

            var bikes = bikesRes.Items?.ToList() ?? new List<Bike>();
            if (!bikes.Any()) return null;

            var listingIds = bikes.Select(b => b.ListingId).Distinct().ToList();

            var listingRes = await _listingRepo.GetAllDataByExpression(
                filter: l => listingIds.Contains(l.Id) && l.UserId == sellerId,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: l => l.CreatedAt,
                isAscending: false
            );

            var ownedListingIds = listingRes.Items?
                .Select(l => l.Id)
                .ToHashSet() ?? new HashSet<Guid>();

            var hasOwnedBike = bikes.Any(b => ownedListingIds.Contains(b.ListingId));
            if (!hasOwnedBike) return null;

            return order;
        }

        private async Task<SellerOrderDto> BuildOrderDtoAsync(Guid sellerId, Order order)
        {
            var orderItemsRes = await _orderItemRepo.GetAllDataByExpression(
                filter: oi => oi.OrderId == order.Id,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: oi => oi.Id,
                isAscending: true
            );

            var orderItems = orderItemsRes.Items?.ToList() ?? new List<OrderItem>();
            var bikeIds = orderItems.Select(x => x.BikeId).Distinct().ToList();

            var bikesRes = await _bikeRepo.GetAllDataByExpression(
                filter: b => bikeIds.Contains(b.Id),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: b => b.CreatedAt,
                isAscending: false
            );

            var bikes = bikesRes.Items?.ToList() ?? new List<Bike>();

            var listingIds = bikes.Select(b => b.ListingId).Distinct().ToList();

            var listingRes = await _listingRepo.GetAllDataByExpression(
                filter: l => listingIds.Contains(l.Id) && l.UserId == sellerId,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: l => l.CreatedAt,
                isAscending: false
            );

            var ownedListingIds = listingRes.Items?
                .Select(l => l.Id)
                .ToHashSet() ?? new HashSet<Guid>();

            var sellerBikeMap = bikes
                .Where(b => ownedListingIds.Contains(b.ListingId))
                .ToDictionary(b => b.Id, b => b);

            var sellerBikeIds = sellerBikeMap.Keys.ToList();

            var mediaRes = await _mediaRepo.GetAllDataByExpression(
                filter: m => sellerBikeIds.Contains(m.BikeId),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: m => m.Id,
                isAscending: true
            );

            var medias = mediaRes.Items?.ToList() ?? new List<Media>();

            var imageMap = medias
                .GroupBy(m => m.BikeId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.Image).FirstOrDefault(img => !string.IsNullOrWhiteSpace(img))
                );

            return new SellerOrderDto
            {
                Id = order.Id,
                Status = order.Status.ToString(),
                ReceiverName = order.ReceiverName,
                ReceiverPhone = order.ReceiverPhone,
                ReceiverAddress = order.ReceiverAddress,
                TotalAmount = order.TotalAmount,
                CreatedAt = DateTimeHelper.ToVN(order.CreatedAt),
                UpdatedAt = order.UpdatedAt.HasValue
    ? DateTimeHelper.ToVN(order.UpdatedAt.Value)
    : null,
                Items = orderItems
                    .Where(oi => sellerBikeIds.Contains(oi.BikeId))
                    .Select(oi =>
                    {
                        sellerBikeMap.TryGetValue(oi.BikeId, out var bike);
                        imageMap.TryGetValue(oi.BikeId, out var image);

                        return new SellerOrderItemDto
                        {
                            Id = oi.Id,
                            BikeId = oi.BikeId,
                            UnitPrice = oi.UnitPrice,
                            LineTotal = oi.LineTotal,
                            BikeBrand = bike?.Brand,
                            BikeCategory = bike?.Category,
                            Image = image
                        };
                    }).ToList()
            };
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Order
{
    public class SellerOrderDetailDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = default!;
        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<SellerOrderBikeDetailDto> Items { get; set; } = new();
    }

    public class SellerOrderBikeDetailDto
    {
        public Guid OrderItemId { get; set; }
        public Guid BikeId { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string? Brand { get; set; }
        public string? Category { get; set; }
        public string? FrameSize { get; set; }
        public string? FrameMaterial { get; set; }
        public string? Paint { get; set; }
        public string? Groupset { get; set; }
        public string? Operating { get; set; }
        public string? TireRim { get; set; }
        public string? BrakeType { get; set; }
        public string? Overall { get; set; }
        public decimal? Price { get; set; }
        public string? BikeStatus { get; set; }

        public Guid ListingId { get; set; }
        public string? ListingTitle { get; set; }
        public string? ListingDescription { get; set; }
        public string? ListingStatus { get; set; }

        public List<string> Images { get; set; } = new();
        public List<string> Videos { get; set; } = new();
    }
}

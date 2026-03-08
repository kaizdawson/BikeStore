using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Order
{
    public class SellerOrderDto
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = default!;

        public string ReceiverName { get; set; } = default!;
        public string ReceiverPhone { get; set; } = default!;
        public string ReceiverAddress { get; set; } = default!;

        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<SellerOrderItemDto> Items { get; set; } = new();
    }
}

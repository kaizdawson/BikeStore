using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Order
{
    public class SellerOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid BikeId { get; set; }

        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public string? BikeBrand { get; set; }
        public string? BikeCategory { get; set; }
        public string? Image { get; set; }
    }
}

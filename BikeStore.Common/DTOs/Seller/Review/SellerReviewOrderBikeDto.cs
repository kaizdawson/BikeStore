using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Review
{
    public class SellerReviewOrderBikeDto
    {
        public Guid BikeId { get; set; }
        public string BikeName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
    }
}

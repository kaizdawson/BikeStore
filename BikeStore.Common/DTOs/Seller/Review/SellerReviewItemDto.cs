using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Review
{
    public class SellerReviewItemDto
    {
        public Guid ReviewId { get; set; }
        public Guid OrderId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = default!;
        public string ReviewerPhone { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
    }
}

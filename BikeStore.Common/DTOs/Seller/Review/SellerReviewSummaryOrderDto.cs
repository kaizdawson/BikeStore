using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Review
{
    public class SellerReviewSummaryOrderDto
    {
        public Guid ReviewId { get; set; }
        public Guid OrderId { get; set; }

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; }

        public Guid ReviewerId { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
        public string ReviewerPhone { get; set; } = string.Empty;

        public string ReceiverName { get; set; } = string.Empty;
        public string ReceiverPhone { get; set; } = string.Empty;
        public string ReceiverAddress { get; set; } = string.Empty;

        public List<SellerReviewOrderBikeDto> Bikes { get; set; } = new();
    }
}

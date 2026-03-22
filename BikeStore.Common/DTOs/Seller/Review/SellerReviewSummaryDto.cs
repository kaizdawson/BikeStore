using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Review
{
    public class SellerReviewSummaryDto
    {
        public int TotalReviews { get; set; }

        public int OneStar { get; set; }
        public int TwoStars { get; set; }
        public int ThreeStars { get; set; }
        public int FourStars { get; set; }
        public int FiveStars { get; set; }

        public int TotalStars { get; set; }
        public decimal AverageRating { get; set; }

        public List<SellerReviewSummaryOrderDto> Orders { get; set; } = new();
    }
}

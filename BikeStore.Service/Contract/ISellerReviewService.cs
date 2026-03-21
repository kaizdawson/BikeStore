using BikeStore.Common.DTOs.Seller.Review;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ISellerReviewService
    {
        Task<List<SellerReviewItemDto>> GetMyReviewsAsync();

        Task<SellerReviewSummaryDto> GetMyReviewSummaryAsync();
    }
}

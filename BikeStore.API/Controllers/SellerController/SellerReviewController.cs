using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SellerReviewController : ControllerBase
    {
        private readonly ISellerReviewService _sellerReviewService;

        public SellerReviewController(ISellerReviewService sellerReviewService)
        {
            _sellerReviewService = sellerReviewService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyReviews()
        {
            try
            {
                var result = await _sellerReviewService.GetMyReviewsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetMyReviewSummary()
        {
            try
            {
                var result = await _sellerReviewService.GetMyReviewSummaryAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}

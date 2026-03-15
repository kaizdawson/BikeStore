using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Buyer;
using BikeStore.Service.Contract;
using BikeStore.Service.Implement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewDto dto)
        {
            try
            {
                var result = await _reviewService.CreateReviewAsync(dto);
                return Ok(new { Success = result, Message = "Gửi đánh giá thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-review/{orderId}")]
        public async Task<IActionResult> GetMyReview(Guid orderId)
        {
            var result = await _reviewService.GetMyReviewByOrderIdAsync(orderId);
            return Ok(result);
        }
    }
}
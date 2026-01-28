using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BikeStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Yêu cầu đăng nhập để sử dụng
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return Ok(await _wishlistService.GetMyWishlistAsync(userId));
        }

        [HttpPost("{bikeId}")]
        public async Task<IActionResult> Add(Guid bikeId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            try
            {
                var result = await _wishlistService.AddToWishlistAsync(userId, bikeId);
                return Ok(new { message = "Đã thêm xe vào danh sách yêu thích thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{bikeId}")]
        public async Task<IActionResult> Delete(Guid bikeId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _wishlistService.RemoveFromWishlistAsync(userId, bikeId);

            if (result)
                return Ok(new { message = "Đã xóa xe khỏi danh sách yêu thích." });

            return BadRequest(new { message = "Không tìm thấy xe trong danh sách yêu thích của bạn." });
        }
    }
}
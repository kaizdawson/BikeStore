using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BikeStore.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemController : ControllerBase
    {
        private readonly ICartItemService _itemService;
        public CartItemController(ICartItemService itemService) => _itemService = itemService;

        [HttpGet("{cartId}")]
        public async Task<IActionResult> GetItems(Guid cartId)
        {
            var result = await _itemService.GetItemsByCartIdAsync(cartId);
            return Ok(result);
        }

        [HttpPost("{bikeId}")]
        public async Task<IActionResult> AddItem(Guid bikeId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var userId = Guid.Parse(userIdClaim);
            try
            {
                var success = await _itemService.AddItemAsync(userId, bikeId);
                return Ok(new { message = "Đã thêm sản phẩm vào giỏ hàng thành công!" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{cartItemId}")]
        public async Task<IActionResult> RemoveItem(Guid cartItemId)
        {
            var result = await _itemService.RemoveItemAsync(cartItemId);
            if (result)
            {
                return Ok(new { message = "Đã xóa món hàng khỏi giỏ." });
            }
            return NotFound(new { message = "Không tìm thấy món hàng để xóa." });
        }

        [HttpPatch("toggle/{cartItemId}")]
        public async Task<IActionResult> ToggleSelection(Guid cartItemId)
        {
            try
            {
                var result = await _itemService.ToggleSelectionAsync(cartItemId);
                return Ok(new { message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("validate/{cartId}")]
        public async Task<IActionResult> ValidateCart(Guid cartId)
        {
            var warnings = await _itemService.ValidateCartAsync(cartId);
            if (warnings.Any())
            {
                return Ok(new
                {
                    message = "Một số sản phẩm trong giỏ hàng có thay đổi.",
                    warnings = warnings
                });
            }
            return Ok(new { message = "Tất cả sản phẩm đều sẵn sàng!" });
        }
    }
}
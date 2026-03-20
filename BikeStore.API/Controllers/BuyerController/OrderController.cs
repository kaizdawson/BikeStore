using BikeStore.Common.DTOs.Buyer;
using BikeStore.Common.Enums;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Authorize] 
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] OrderDto dto)
        {
            try
            {
                var orderId = await _orderService.CreateOrderAsync(dto);
                return Ok(new { Success = true, OrderId = orderId, Message = "Đặt hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var orders = await _orderService.GetMyOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var order = await _orderService.GetOrderDetailAsync(id);
            if (order == null) return NotFound(new { Message = "Không tìm thấy đơn hàng." });
            return Ok(order);
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            try
            {
                var result = await _orderService.CancelOrderAsync(id);
                return Ok(new { Success = result, Message = "Hủy đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [Authorize(Roles = "ADMIN")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllOrders()
        {
            var orders = await _orderService.GetAllOrdersAsync();
            return Ok(orders);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderStatusEnum status)
        {
            try
            {
                var result = await _orderService.UpdateStatusAsync(id, status);
                return Ok(new { Success = result, Message = "Cập nhật trạng thái đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("buy-now/{bikeId}")]
        public async Task<IActionResult> BuyNow(Guid bikeId, [FromBody] OrderDto dto)
        {
            try
            {
                var orderId = await _orderService.BuyNowAsync(bikeId, dto);
                return Ok(new { OrderId = orderId, Message = "Tạo đơn hàng mua ngay thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
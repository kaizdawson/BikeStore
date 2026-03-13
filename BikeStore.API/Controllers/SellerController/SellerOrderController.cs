using BikeStore.API.Helpers;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerController
{
    [Route("api/seller/orders")]
    [ApiController]
    [Authorize(Roles = "SELLER")]
    public class SellerOrderController : ControllerBase
    {
        private readonly ISellerOrderService _service;

        public SellerOrderController(ISellerOrderService service)
        {
            _service = service;
        }

        [HttpGet("paid")]
        public async Task<IActionResult> GetPaidOrders(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            var sellerId = ClaimsHelper.GetUserId(HttpContext);
            var res = await _service.GetPaidOrdersAsync(sellerId, pageNumber, pageSize);
            return Ok(res);
        }

        [HttpPut("confirm")]
        public async Task<IActionResult> ConfirmOrder([FromHeader] Guid orderId)
        {
            var sellerId = ClaimsHelper.GetUserId(HttpContext);
            var res = await _service.ConfirmOrderAsync(sellerId, orderId);

            if (res == null)
                return NotFound(new
                {
                    message = "Order không tồn tại, không thuộc xe của bạn, hoặc không ở trạng thái Paid."
                });

            return Ok(res);
        }

        [HttpPut("shipping")]
        public async Task<IActionResult> ShipOrder([FromHeader] Guid orderId)
        {
            var sellerId = ClaimsHelper.GetUserId(HttpContext);
            var res = await _service.ShipOrderAsync(sellerId, orderId);

            if (res == null)
                return NotFound(new
                {
                    message = "Order không tồn tại, không thuộc xe của bạn, hoặc không ở trạng thái Confirmed."
                });

            return Ok(res);
        }

        [HttpPut("complete")]
        public async Task<IActionResult> CompleteOrder([FromHeader] Guid orderId)
        {
            var sellerId = ClaimsHelper.GetUserId(HttpContext);
            var res = await _service.CompleteOrderAsync(sellerId, orderId);

            if (res == null)
                return NotFound(new
                {
                    message = "Order không tồn tại, không thuộc xe của bạn, hoặc không ở trạng thái Shipping."
                });

            return Ok(res);
        }

        [HttpGet("item/{orderItemId}")]
        public async Task<IActionResult> GetOrderItemDetails([FromRoute] Guid orderItemId)
        {
            var sellerId = ClaimsHelper.GetUserId(HttpContext);
            var res = await _service.GetOrderItemDetailsAsync(sellerId, orderItemId);

            if (res == null)
                return NotFound(new
                {
                    message = "Order item không tồn tại hoặc không thuộc xe của bạn."
                });

            return Ok(res);
        }

    }
}
using BikeStore.API.Helpers;
using BikeStore.Common.DTOs.PayOs;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.PayOsController;

[Route("api/payos")]
[ApiController]
[Authorize] 
public class PayOsController : ControllerBase
{
    private readonly IPayOsCheckoutService _service;

    public PayOsController(IPayOsCheckoutService service)
    {
        _service = service;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutOrderDto dto)
    {
        var userId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.CheckoutAsync(userId, dto.OrderId);
        return Ok(res);
    }
}
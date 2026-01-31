using BikeStore.API.Helpers;
using BikeStore.Common.DTOs.Inspector;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.InspectorController;

[Route("api/inspector")]
[ApiController]
[Authorize(Roles = "INSPECTOR")]
public class InspectorController : ControllerBase
{
    private readonly IInspectorService _service;

    public InspectorController(IInspectorService service)
    {
        _service = service;
    }

    
    [HttpGet("pending-bikes")]
    public async Task<IActionResult> GetPendingBikes(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var res = await _service.GetPendingBikesAsync(pageNumber, pageSize);
        return Ok(res);
    }

    
    [HttpPost("approve-bike")]
    public async Task<IActionResult> ApproveBike(
        [FromHeader(Name = "x-bike-id")] Guid bikeId,
        [FromBody] ApproveBikeDto dto)
    {
        if (bikeId == Guid.Empty)
            return BadRequest(new { success = false, message = "Thiếu header x-bike-id." });

        var inspectorId = ClaimsHelper.GetUserId(HttpContext);

        var (ok, msg) = await _service.ApproveBikeAsync(inspectorId, bikeId, dto);
        return Ok(new { success = ok, message = msg });
    }
}
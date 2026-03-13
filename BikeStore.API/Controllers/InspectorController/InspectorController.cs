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

    [HttpGet("pending-bike-details")]
    public async Task<IActionResult> GetPendingBikeDetails(
    [FromHeader(Name = "x-pendingbike-id")] Guid pendingBikeId)
    {
        if (pendingBikeId == Guid.Empty)
            return BadRequest(new { message = "Thiếu header x-pendingbike-id." });

        var res = await _service.GetPendingBikeDetailsAsync(pendingBikeId);

        if (res == null)
            return NotFound(new { message = "Không tìm thấy bike pending." });

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

    [HttpPost("reject-bike")]
    public async Task<IActionResult> RejectBike(
    [FromHeader(Name = "x-bike-id")] Guid bikeId,
    [FromBody] RejectBikeDto dto)
    {
        if (bikeId == Guid.Empty)
            return BadRequest(new { success = false, message = "Thiếu header x-bike-id." });

        var inspectorId = ClaimsHelper.GetUserId(HttpContext);

        var (ok, msg) = await _service.RejectBikeAsync(inspectorId, bikeId, dto?.Comment);
        return Ok(new { success = ok, message = msg });
    }
}
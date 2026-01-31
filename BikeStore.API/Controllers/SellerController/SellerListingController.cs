using BikeStore.API.Helpers;
using BikeStore.Common.DTOs.Seller.Listing;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerController;


[Route("api/seller/listings")]
[ApiController]
[Authorize(Roles = "SELLER")]
public class SellerListingController : ControllerBase
{
    private readonly ISellerListingService _service;

    public SellerListingController(ISellerListingService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ListingUpsertDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.CreateAsync(sellerId, dto);
        return Ok(res);
    }

    [HttpGet]
    public async Task<IActionResult> GetMyListings([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetMyListingsAsync(sellerId, pageNumber, pageSize);
        return Ok(res);
    }

    [HttpGet("{listingId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid listingId)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetByIdAsync(sellerId, listingId);
        if (res == null) return NotFound(new { message = "Listing không tồn tại hoặc không thuộc quyền." });
        return Ok(res);
    }

    [HttpPut("{listingId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid listingId, [FromBody] ListingUpsertDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.UpdateAsync(sellerId, listingId, dto);
        if (res == null) return NotFound(new { message = "Listing không tồn tại hoặc không thuộc quyền." });
        return Ok(res);
    }

    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid listingId)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var ok = await _service.DeleteAsync(sellerId, listingId);
        if (!ok) return NotFound(new { message = "Listing không tồn tại hoặc không thuộc quyền." });
        return Ok(new { success = true });
    }

    [HttpGet("{listingId:guid}/details")]
    public async Task<IActionResult> GetDetails([FromRoute] Guid listingId)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetDetailsAsync(sellerId, listingId);
        if (res == null) return NotFound(new { message = "Listing không tồn tại hoặc không thuộc quyền." });
        return Ok(res);
    }
}
using BikeStore.API.Helpers;
using BikeStore.Common.DTOs.Seller.Bike;
using BikeStore.Common.DTOs.Seller.Media;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerController;

[Route("api/seller/bikes")]
[ApiController]
[Authorize(Roles = "SELLER")]
public class SellerBikeController : ControllerBase
{
    private readonly ISellerBikeService _service;
    private readonly ICloudinaryService _cloud;

    public SellerBikeController(ISellerBikeService service, ICloudinaryService cloud)
    {
        _service = service;
        _cloud = cloud;
    }


    [HttpGet]
    public async Task<IActionResult> GetMyBikes(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] Guid? listingId = null,
    [FromQuery] string? status = null
    )
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetMyBikesAsync(sellerId, pageNumber, pageSize, listingId, status);
        return Ok(res);
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromHeader(Name = "x-listing-id")] Guid listingId,
        [FromBody] BikeUpsertDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.CreateAsync(sellerId, listingId, dto);
        return Ok(res);
    }

    [HttpGet("{bikeId:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid bikeId)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetByIdAsync(sellerId, bikeId);
        if (res == null) return NotFound(new { message = "Bike không tồn tại hoặc không thuộc quyền." });
        return Ok(res);
    }

    [HttpGet("by-listing/{listingId:guid}")]
    public async Task<IActionResult> GetByListing(
        [FromRoute] Guid listingId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.GetByListingAsync(sellerId, listingId, pageNumber, pageSize);
        return Ok(res);
    }

    [HttpPut("{bikeId:guid}")]
    public async Task<IActionResult> Update([FromRoute] Guid bikeId, [FromBody] BikeUpsertDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _service.UpdateAsync(sellerId, bikeId, dto);
        if (res == null) return NotFound(new { message = "Bike không tồn tại hoặc không thuộc quyền." });
        return Ok(res);
    }

    [HttpDelete("{bikeId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid bikeId)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var ok = await _service.DeleteAsync(sellerId, bikeId);
        if (!ok) return NotFound(new { message = "Bike không tồn tại hoặc không thuộc quyền." });
        return Ok(new { success = true });
    }

    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadImage(
    [FromHeader(Name = "x-bike-id")] Guid bikeId,
    [FromForm] UploadMediaDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _cloud.UploadBikeImageAndSaveAsync(sellerId, bikeId, dto.File);
        return Ok(res);
    }

    [HttpPost("upload-video")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVideo(
        [FromHeader(Name = "x-bike-id")] Guid bikeId,
        [FromForm] UploadMediaDto dto)
    {
        var sellerId = ClaimsHelper.GetUserId(HttpContext);
        var res = await _cloud.UploadBikeVideoAndSaveAsync(sellerId, bikeId, dto.File);
        return Ok(res);
    }
}
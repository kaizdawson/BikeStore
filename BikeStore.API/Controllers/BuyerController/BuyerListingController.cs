using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Route("api/buyer/listings")] 
    [ApiController]
    public class BuyerListingController : ControllerBase
    {
        private readonly IBuyerListingService _buyerListingService;

        public BuyerListingController(IBuyerListingService buyerListingService)
        {
            _buyerListingService = buyerListingService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAvailable([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _buyerListingService.GetAllAvailableBikesAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var result = await _buyerListingService.GetListingDetailByListingIdAsync(id);

            if (result == null)
            {
                return NotFound(new { Message = "Tin đăng không tồn tại hoặc đã bị gỡ." });
            }

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] string name, int pageNumber = 1, int pageSize = 12)
        {
            var result = await _buyerListingService.SearchBikesByNameAsync(name, pageNumber, pageSize);
            return Ok(result);
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterByTags([FromBody] List<string> tags, [FromQuery] int pageNumber = 1, int pageSize = 12)
        {
            var result = await _buyerListingService.FilterBikesByTagsAsync(tags, pageNumber, pageSize);
            return Ok(result);
        }
    }
}
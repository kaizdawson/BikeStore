using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Route("api/buyer-bikes")]
    [ApiController]
    [Authorize(Roles = "BUYER")]
    public class BuyerBikeController : ControllerBase
    {
        private readonly IBuyerBikeService _buyerBikeService;

        public BuyerBikeController(IBuyerBikeService buyerBikeService)
        {
            _buyerBikeService = buyerBikeService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAvailable([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _buyerBikeService.GetAllAvailableBikesAsync(pageNumber, pageSize);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var bike = await _buyerBikeService.GetBikeDetailAsync(id);

            if (bike == null)
            {
                return NotFound(new { Message = "Sản phẩm không tồn tại." });
            }

            return Ok(bike);
        }
    }
}
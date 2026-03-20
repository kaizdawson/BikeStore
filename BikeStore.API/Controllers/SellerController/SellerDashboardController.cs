using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerDashboardController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "SELLER")]
    public class SellerDashboardController : ControllerBase
    {
        private readonly ISellerDashboardService _sellerDashboardService;

        public SellerDashboardController(ISellerDashboardService sellerDashboardService)
        {
            _sellerDashboardService = sellerDashboardService;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardOverview()
        {
            try
            {
                var result = await _sellerDashboardService.GetSellerDashboardAsync();
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = "Lỗi khi tải dữ liệu Dashboard", Detail = ex.Message });
            }
        }
    }
   }

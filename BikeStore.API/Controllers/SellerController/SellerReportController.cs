using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.SellerController
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SellerReportController : ControllerBase
    {
        private readonly ISellerReportService _sellerReportService;

        public SellerReportController(ISellerReportService sellerReportService)
        {
            _sellerReportService = sellerReportService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyOrderReports()
        {
            try
            {
                var result = await _sellerReportService.GetMyOrderReportsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{reportId}/processing")]
        public async Task<IActionResult> MarkProcessing(Guid reportId)
        {
            try
            {
                var result = await _sellerReportService.MarkProcessingAsync(reportId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{reportId}/resolved")]
        public async Task<IActionResult> MarkResolved(Guid reportId)
        {
            try
            {
                var result = await _sellerReportService.MarkResolvedAsync(reportId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPut("{reportId}/rejected")]
        public async Task<IActionResult> MarkRejected(Guid reportId)
        {
            try
            {
                var result = await _sellerReportService.MarkRejectedAsync(reportId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
    }
}

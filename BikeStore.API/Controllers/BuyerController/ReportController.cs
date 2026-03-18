using BikeStore.Common.DTOs.Buyer;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.BuyerController
{
    [Authorize(Roles = "BUYER")]
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportController(IReportService buyerService)
        {
            _reportService = buyerService;
        }

        // API Gửi báo cáo đơn hàng
        // POST: /api/BuyerReport/send-report
        [HttpPost("send-report")]
        public async Task<IActionResult> SendReport([FromBody] ReportDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
                return BadRequest(new { Message = "Lý do báo cáo không được để trống." });

            var (success, message) = await _reportService.CreateReportAsync(dto);

            if (!success) return BadRequest(new { Message = message });

            return Ok(new { Message = message });
        }
    }
}

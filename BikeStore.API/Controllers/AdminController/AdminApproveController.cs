using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.AdminController
{
    [Authorize(Roles = "ADMIN")]
    [ApiController]
    [Route("api/admin/approve")]
    public class AdminApproveController : ControllerBase
    {
        private readonly IAdminApproveService _approveService;

        public AdminApproveController(IAdminApproveService approveService)
        {
            _approveService = approveService;
        }

        [HttpGet("pending-list")]
        public async Task<IActionResult> GetPending()
            => Ok(await _approveService.GetPendingListingsAsync());

        [HttpPatch("{id}")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] AdminApproveDto dto)
        {
            try
            {
                var success = await _approveService.ApproveListingAsync(id, dto);
                return Ok(new
                {
                    Success = success,
                    Message = dto.IsApproved ? "Duyệt thành công (Active)" : "Đã từ chối (Rejected)"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
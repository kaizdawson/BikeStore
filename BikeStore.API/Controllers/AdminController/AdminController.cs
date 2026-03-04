using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.AdminController
{
    [Authorize(Roles = "ADMIN")]
    [ApiController]
    [Route("api/admin/listing")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _approveService;

        public AdminController(IAdminService approveService)
        {
            _approveService = approveService;
        }

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

        [HttpGet("detail/{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var detail = await _approveService.GetListingDetailAsync(id);
            if (detail == null) return NotFound(new { Message = "Không tìm thấy tin đăng." });
            return Ok(detail);
        }


        [HttpGet("pending-list")]
        public async Task<IActionResult> GetPending()
            => Ok(await _approveService.GetPendingListingsAsync());

        [HttpGet("inspecting-list")]
        public async Task<IActionResult> GetInspecting()
            => Ok(await _approveService.GetInspectingListingsAsync());

        [HttpGet("active-list")]
        public async Task<IActionResult> GetActive()
            => Ok(await _approveService.GetActiveListingsAsync());

        [HttpGet("rejected-list")]
        public async Task<IActionResult> GetRejected()
            => Ok(await _approveService.GetRejectedListingsAsync());

        [HttpPost("create-inspector")]
        public async Task<IActionResult> CreateInspector([FromBody] SignUpDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _approveService.CreateInspectorAsync(dto);

            if (!result.Success) return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }
    }
}
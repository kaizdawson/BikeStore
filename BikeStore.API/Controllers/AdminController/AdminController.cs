using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Common.Enums;
using BikeStore.Service.Contract;
using BikeStore.Service.Implementation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.AdminController
{
    [Authorize(Roles = "ADMIN")]
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService approveService)
        {
            _adminService = approveService;
        }

        [HttpPatch("listing/approve/{id}")]
        public async Task<IActionResult> Approve(Guid id, [FromBody] AdminApproveDto dto)
        {
            try
            {
                var success = await _adminService.ApproveListingAsync(id, dto);
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

        [HttpGet("listing/detail/{id}")]
        public async Task<IActionResult> GetDetail(Guid id)
        {
            var detail = await _adminService.GetListingDetailAsync(id);
            if (detail == null) return NotFound(new { Message = "Không tìm thấy tin đăng." });
            return Ok(detail);
        }


        [HttpGet("listing/pending-list")]
        public async Task<IActionResult> GetPending()
            => Ok(await _adminService.GetPendingListingsAsync());

        [HttpGet("listing/inspecting-list")]
        public async Task<IActionResult> GetInspecting()
            => Ok(await _adminService.GetInspectingListingsAsync());

        [HttpGet("listing/active-list")]
        public async Task<IActionResult> GetActive()
            => Ok(await _adminService.GetActiveListingsAsync());

        [HttpGet("listing/rejected-list")]
        public async Task<IActionResult> GetRejected()
            => Ok(await _adminService.GetRejectedListingsAsync());

        [HttpPost("create-inspector")]
        public async Task<IActionResult> CreateInspector([FromBody] SignUpDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _adminService.CreateInspectorAsync(dto);

            if (!result.Success) return BadRequest(new { Message = result.Message });

            return Ok(new { Message = result.Message });
        }

        [HttpGet("list-user")]
        public async Task<IActionResult> GetUsers(
             string? search,
             RoleEnum? role,           
             UserStatusEnum? status,   
             int page = 1,
             int size = 10)
        {
            var result = await _adminService.GetUsersManagerAsync(search, role, status, page, size);
            return Ok(result);
        }

        [HttpPut("ban-user/{id}")]
        public async Task<IActionResult> BanUser(Guid id)
        {
            var success = await _adminService.BanUserAsync(id);

            if (!success)
            {
                return BadRequest(new { Message = "Không thể cập nhật trạng thái người dùng hoặc người dùng không tồn tại." });
            }

            return Ok(new { Message = "Cập nhật trạng thái người dùng thành công." });
        }
    }
}
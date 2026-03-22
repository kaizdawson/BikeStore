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

        [HttpGet("list-brands")]
        public async Task<IActionResult> GetBrandStats(string? search, int page = 1, int size = 10)
        {
            return Ok(await _adminService.GetBrandStatisticsAsync(search, page, size));
        }
        [HttpGet("list-categories")]
        public async Task<IActionResult> GetCategoryStats(string? search, int page = 1, int size = 10)

        {
            var result = await _adminService.GetCategoryStatisticsAsync(search, page, size);
            return Ok(result);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardOverview()
        {
            try
            {
                var result = await _adminService.GetDashboardOverviewAsync();

                if (result == null)
                {
                    return NotFound(new { Message = "Không tìm thấy dữ liệu thống kê." });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Đã xảy ra lỗi khi lấy dữ liệu Dashboard.",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetAllTransactions()
        {
            try
            {
                var result = await _adminService.GetTransactionsForAdminAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Đã xảy ra lỗi khi lấy danh sách giao dịch.",
                    Detail = ex.Message
                });
            }
        }

        [HttpGet("list-reports")]
        public async Task<IActionResult> GetReports()
        {
            try
            {
                var result = await _adminService.GetReportsForAdminAsync();

                if (result == null) return Ok(new List<object>());

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Message = "Đã xảy ra lỗi khi lấy danh sách khiếu nại.",
                    Detail = ex.Message
                });
            }
        }

        [HttpPut("{reportId}/progress")]
        public async Task<IActionResult> ProgressReport(Guid reportId)
        {
            try
            {
                var result = await _adminService.ProgressReportStatusAsync(reportId);
                return Ok(new { Message = "Cập nhật tiến độ báo cáo thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{reportId}/reject")]
        public async Task<IActionResult> RejectReport(Guid reportId)
        {
            try
            {
                var result = await _adminService.RejectReportAsync(reportId);
                return Ok(new { Message = "Đã từ chối báo cáo." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("withdrawals")]
        public async Task<IActionResult> GetAllWithdrawals()
        {
            try
            {
                var data = await _adminService.GetWithdrawalRequestsAsync();
                
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("withdrawals/{id}/approve")]
        public async Task<IActionResult> ApproveWithdrawal(Guid id)
        {
            try
            {
                await _adminService.ApproveWithdrawalAsync(id);
                return Ok(new { Message = "Đã xác nhận thanh toán rút tiền thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("withdrawals/{id}/reject")]
        public async Task<IActionResult> RejectWithdrawal(Guid id)
        {
            try
            {
                 await _adminService.RejectWithdrawalAsync(id);
                return Ok(new { Message = "Đã từ chối yêu cầu và hoàn trả lại tiền vào số dư của User." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
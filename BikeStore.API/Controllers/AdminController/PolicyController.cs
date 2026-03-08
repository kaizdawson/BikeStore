using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PolicyController : ControllerBase
    {
        private readonly IPolicyService _policyService;

        public PolicyController(IPolicyService policyService)
        {
            _policyService = policyService;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var result = await _policyService.GetAllPoliciesAsync();
            return Ok(result);
        }

        [HttpGet("current")]
        public async Task<IActionResult> GetCurrent()
        {
            var result = await _policyService.GetCurrentActivePolicyAsync();
            if (result == null) return NotFound("Không tìm thấy chính sách nào đang có hiệu lực.");
            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PolicyCreateDto dto)
        {
            try
            {
                var result = await _policyService.CreatePolicyAsync(dto);
                return Ok(new { Success = result, Message = "Tạo chính sách thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] PolicyUpdateDto dto)
        {
            try
            {
                var result = await _policyService.UpdatePolicyAsync(id, dto);
                return Ok(new { Success = result, Message = "Cập nhật chính sách thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var result = await _policyService.DeletePolicyAsync(id);
                return Ok(new { Success = result, Message = "Xóa chính sách thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
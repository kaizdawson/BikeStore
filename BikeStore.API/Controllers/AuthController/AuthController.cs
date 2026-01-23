using BikeStore.Common.DTOs;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;

namespace BikeStore.API.Controllers.AuthController
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _auth;

        public AuthController(IAuthService auth)
        {
            _auth = auth;
        }

        private string? Ip() => HttpContext.Connection.RemoteIpAddress?.ToString();
        private string? Device() => Request.Headers.UserAgent.ToString();

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
        {
            var res = await _auth.SignUpAsync(dto);
            return Ok(res);
        }


        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] SendOtpDto dto)
        {
            var res = await _auth.ResendOtpAsync(dto.Email);
            return Ok(res);
        }


        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerifyDto dto)
        {
            var (ok, msg) = await _auth.VerifyOtpAsync(dto);
            return Ok(new { success = ok, message = msg });
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] LoginDto dto)
        {
            var res = await _auth.SignInAsync(dto, Ip(), Device());
            return Ok(res);
        }



        [HttpPost("renew-token")]
        public async Task<IActionResult> RenewToken([FromBody] RenewTokenDto dto)
        {
            var res = await _auth.RenewTokenAsync(dto.RefreshToken, Ip(), Device());
            return Ok(res);
        }


    }
}

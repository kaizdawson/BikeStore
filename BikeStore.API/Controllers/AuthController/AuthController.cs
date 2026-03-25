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

            if (res.Success)
            {
                Response.Cookies.Append("refreshToken", res.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            return Ok(res);
        }



        [HttpPost("renew-token")]
        public async Task<IActionResult> RenewToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var res = await _auth.RenewTokenAsync(refreshToken ?? "", Ip(), Device());

            if (res.Success)
            {
                Response.Cookies.Append("refreshToken", res.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, 
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });
            }

            return Ok(res);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];

            var (success, message, errorType) = await _auth.LogoutAsync(refreshToken ?? "");

            Response.Cookies.Delete("refreshToken");

            return Ok(new
            {
                success,
                message,
                errorType
            });
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto dto)
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var deviceInfo = Request.Headers["User-Agent"].ToString();

            var result = await _auth.GoogleSignInAsync(dto.IdToken, dto.Role, ipAddress, deviceInfo);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Path = "/",
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return Ok(new
            {
                success = result.Success,
                token = result.Token,
                refreshToken = result.RefreshToken,
                message = result.Message,
                role = result.Role
            });
        }

    }
}

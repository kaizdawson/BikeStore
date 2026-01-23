using BikeStore.Common.DTOs;
using Microsoft.AspNetCore.Identity.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IAuthService
    {
        Task<SignUpResult> SignUpAsync(SignUpDto dto);

        Task<LoginResult> SignInAsync(LoginDto request, string? ipAddress, string? deviceInfo);

        Task<LoginResult> RenewTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo);

        Task SendOtpAsync(string email);
        Task<(bool Success, string Message)> VerifyOtpAsync(OtpVerifyDto dto);

        Task<SignUpResult> ResendOtpAsync(string email);
    }

}

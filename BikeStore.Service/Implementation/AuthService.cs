using BikeStore.Common.DTOs;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using BikeStore.Service.Helpers;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Implementation
{

    public class AuthService : IAuthService
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<RefreshToken> _refreshRepo;
        private readonly IUnitOfWork _uow;
        private readonly IJwtService _jwt;
        private readonly IEmailService _email;
        private readonly IMemoryCache _cache;

        private const int OTP_MINUTES = 2;
        private const int REFRESH_DAYS = 7;

        public AuthService(
            IGenericRepository<User> userRepo,
            IGenericRepository<RefreshToken> refreshRepo,
            IUnitOfWork uow,
            IJwtService jwt,
            IEmailService email,
            IMemoryCache cache)
        {
            _userRepo = userRepo;
            _refreshRepo = refreshRepo;
            _uow = uow;
            _jwt = jwt;
            _email = email;
            _cache = cache;
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLower();
        private static string OtpCacheKey(string email) => $"otp:{NormalizeEmail(email)}";

        public async Task<SignUpResult> SignUpAsync(SignUpDto dto)
        {
            var email = NormalizeEmail(dto.Email);

            if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                var existedPhone = await _userRepo.GetFirstByExpression(u => u.PhoneNumber == dto.PhoneNumber);
                if (existedPhone != null)
                    return new SignUpResult { Success = false, Message = "Số điện thoại này đã tồn tại." };
            }

            var existed = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (existed != null)
                return new SignUpResult { Success = false, Message = "Email này đã tồn tại." };

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = dto.FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                Email = email,
                Password = PasswordHasher.Hash(dto.Password),
                Role = dto.Role,
                Status = UserStatusEnum.InActive,
                WalletBalance = 0m
            };

            await _userRepo.Insert(user);
            await _uow.SaveChangeAsync();

            await SendOtpAsync(email);

            return new SignUpResult
            {
                Success = true,
                Message = "Đăng ký thành công. Vui lòng nhập mã OTP để xác thực Email."
            };
        }


        public async Task SendOtpAsync(string email)
        {
            var normalized = NormalizeEmail(email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == normalized);
            if (user == null) return;

            if (user.Status == UserStatusEnum.Active) return; 

            var otp = OtpGenerator.GenerateOtp(6);
            _cache.Set(OtpCacheKey(normalized), otp, TimeSpan.FromMinutes(OTP_MINUTES));

            await _email.SendEmailAsync(
                normalized,
                "Xác thực tài khoản BikeStore",
                $"Mã OTP của bạn là: {otp} (hết hạn trong {OTP_MINUTES} phút)"
            );
        }

        public async Task<(bool Success, string Message)> VerifyOtpAsync(OtpVerifyDto dto)
        {
            var email = NormalizeEmail(dto.Email);
            var key = OtpCacheKey(email);

            if (!_cache.TryGetValue(key, out string? cachedOtp) || cachedOtp != dto.Otp)
                return (false, "OTP không hợp lệ hoặc đã hết hạn.");

            var user = await _userRepo.GetFirstByExpression(u => u.Email == email);
            if (user == null) return (false, "Người dùng không tồn tại.");

            if (user.Status == UserStatusEnum.Banned)
                return (false, "Tài khoản đã bị khóa.");

            user.Status = UserStatusEnum.Active;
            await _userRepo.Update(user);
            await _uow.SaveChangeAsync();

            _cache.Remove(key);
            return (true, "Xác thực thành công! Tài khoản đã được Active.");
        }

        public async Task<LoginResult> SignInAsync(LoginDto request, string? ipAddress, string? deviceInfo)
{
    var email = NormalizeEmail(request.Email);

    var user = await _userRepo.GetFirstByExpression(u => u.Email == email);
    if (user == null)
        return new LoginResult { Success = false, Message = "Email này chưa được đăng ký." };

    if (user.Status == UserStatusEnum.InActive)
        return new LoginResult { Success = false, Message = "Tài khoản chưa xác minh email. Vui lòng nhập OTP để kích hoạt." };

    if (user.Status == UserStatusEnum.Banned)
        return new LoginResult { Success = false, Message = "Tài khoản đã bị khóa." };

    if (!PasswordHasher.Verify(request.Password, user.Password))
        return new LoginResult { Success = false, Message = "Mật khẩu không đúng." };

    var oldTokens = await _refreshRepo.GetAllDataByExpression(
        r => r.UserId == user.Id && !r.Revoked,
        0, 0, null, true
    );

    if (oldTokens.Items != null)
    {
        foreach (var old in oldTokens.Items)
        {
            old.Revoked = true;
            await _refreshRepo.Update(old);
        }
    }

    var refreshToken = RefreshTokenGenerator.Generate();

    var refreshEntity = new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        Token = refreshToken,
        CreatedAt = DateTimeHelper.NowVN(),
        ExpiredAt = DateTimeHelper.NowVN().AddDays(REFRESH_DAYS),
        Revoked = false,
        IpAddress = ipAddress ?? "unknown",
        DeviceInfo = deviceInfo ?? "unknown"
    };

    await _refreshRepo.Insert(refreshEntity);
    await _uow.SaveChangeAsync();

    var (accessToken, _) = _jwt.GenerateAccessToken(user);

    return new LoginResult
    {
        Success = true,
        Message = "Đăng nhập thành công",
        Token = accessToken,
        RefreshToken = refreshToken,
        Role = user.Role.ToString()
    };
}


        public async Task<LoginResult> RenewTokenAsync(string refreshToken, string? ipAddress, string? deviceInfo)
        {
            var stored = await _refreshRepo.GetFirstByExpression(r => r.Token == refreshToken);

            if (stored == null || stored.Revoked)
                return new LoginResult { Success = false, Message = "Refresh token không hợp lệ." };

            if (stored.ExpiredAt <= DateTimeHelper.NowVN())
            {
                stored.Revoked = true;
                await _refreshRepo.Update(stored);
                await _uow.SaveChangeAsync();
                return new LoginResult { Success = false, Message = "Refresh token đã hết hạn." };
            }

            var user = await _userRepo.GetById(stored.UserId);
            if (user == null)
                return new LoginResult { Success = false, Message = "Người dùng không tồn tại." };

            if (user.Status != UserStatusEnum.Active)
                return new LoginResult { Success = false, Message = "Tài khoản chưa được kích hoạt." };

           
            stored.Revoked = true;
            stored.IpAddress = ipAddress ?? stored.IpAddress;
            stored.DeviceInfo = deviceInfo ?? stored.DeviceInfo;
            await _refreshRepo.Update(stored);

            var newRefresh = RefreshTokenGenerator.Generate();
            var newEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefresh,
                CreatedAt = DateTimeHelper.NowVN(),
                ExpiredAt = DateTimeHelper.NowVN().AddDays(REFRESH_DAYS),
                Revoked = false,
                IpAddress = ipAddress ?? "unknown",
                DeviceInfo = deviceInfo ?? "unknown"
            };
            await _refreshRepo.Insert(newEntity);

            var (newAccess, _) = _jwt.GenerateAccessToken(user);
            await _uow.SaveChangeAsync();

            return new LoginResult
            {
                Success = true,
                Message = "Renew thành công",
                Token = newAccess,
                RefreshToken = newRefresh,
                Role = user.Role.ToString()
            };
        }

        public async Task<SignUpResult> ResendOtpAsync(string email)
        {
            var normalized = NormalizeEmail(email);

            var user = await _userRepo.GetFirstByExpression(u => u.Email == normalized);
            if (user == null)
                return new SignUpResult { Success = false, Message = "Email chưa được đăng ký." };

            if (user.Status == UserStatusEnum.Banned)
                return new SignUpResult { Success = false, Message = "Tài khoản đã bị khóa." };

            if (user.Status == UserStatusEnum.Active)
                return new SignUpResult { Success = false, Message = "Tài khoản đã xác thực email rồi." };

            
            await SendOtpAsync(normalized);

            return new SignUpResult
            {
                Success = true,
                Message = "Đã gửi lại mã OTP. Vui lòng kiểm tra email để xác thực."
            };
        }

    }
}

using System.ComponentModel.DataAnnotations;

namespace BikeStore.Common.DTOs
{
    public class OtpVerifyDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;

        [Required]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP phải đúng 6 chữ số.")]
        public string Otp { get; set; } = null!;
    }
}

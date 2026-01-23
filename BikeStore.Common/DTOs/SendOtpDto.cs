using System.ComponentModel.DataAnnotations;

namespace BikeStore.Common.DTOs
{
    public class SendOtpDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = null!;
    }
}

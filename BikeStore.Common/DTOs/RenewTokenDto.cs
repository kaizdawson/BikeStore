using System.ComponentModel.DataAnnotations;

namespace BikeStore.Common.DTOs
{
    public class RenewTokenDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }
}

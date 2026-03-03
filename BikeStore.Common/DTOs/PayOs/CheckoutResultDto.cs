using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.PayOs
{
    public class CheckoutResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public CheckoutResponseDto? Data { get; set; }
    }
}

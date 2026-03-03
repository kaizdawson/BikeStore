using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.PayOs
{
    public class CheckoutResponseDto
    {
        public string OrderCode { get; set; } = null!;
        public string CheckoutUrl { get; set; } = null!;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs
{
    public class LogoutDto
    {
        public string RefreshToken { get; set; } = string.Empty;
    }
}

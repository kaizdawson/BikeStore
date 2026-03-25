using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs
{
    public class GoogleLoginRequestDto
    {
        public string IdToken { get; set; } = default!;
        public RoleEnum Role { get; set; }
    }
}

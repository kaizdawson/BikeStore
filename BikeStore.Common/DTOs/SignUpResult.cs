using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs
{
    public class SignUpResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}

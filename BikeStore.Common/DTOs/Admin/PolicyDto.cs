using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Admin
{
    public class PolicyDto
    {
        public string Description { get; set; } = default!;
        public decimal PercentOfSystem { get; set; }
        public decimal PercentOfSeller { get; set; }
        public DateTime AppliedDate { get; set; } 
    }
}

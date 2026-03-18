using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Buyer
{
    public class ReportDto
    {
        public Guid OrderId { get; set; }
        public ReportTypeEnum Type { get; set; }
        public string Reason { get; set; } = default!;
    }
}

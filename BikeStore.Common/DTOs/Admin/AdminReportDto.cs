using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Admin
{
    public class AdminReportDto
    {
        public Guid ReportId { get; set; }
        public string ReportCode { get; set; } 
        public string CreatedAt { get; set; }

        public string ReporterName { get; set; }
        public string ReporterPhone { get; set; }
        public string BikeTitle { get; set; }
        public string BikeCode { get; set; } 
        public string SellerName { get; set; }

        public string ReportType { get; set; }

        public string Reason { get; set; }
        public string Status { get; set; } 
        public Guid OrderId { get; set; }
    }
}

using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Report
{
    public class ReportSellerItemDto
    {
        public Guid ReportId { get; set; }
        public Guid OrderId { get; set; }

        public ReportTypeEnum Type { get; set; }
        public string Reason { get; set; } = default!;
        public ReportStatusEnum Status { get; set; }

        public Guid ReporterId { get; set; }
        public string ReporterName { get; set; } = default!;
        public string ReporterPhone { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

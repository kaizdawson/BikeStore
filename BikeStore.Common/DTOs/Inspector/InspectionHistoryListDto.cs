using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Inspector
{
    public class InspectionHistoryListDto
    {
        public Guid InspectionId { get; set; }
        public Guid BikeId { get; set; }
        public Guid ListingId { get; set; }

        public string BikeName { get; set; } = default!;
        public string BikeCode { get; set; } = default!;
        public string? Thumbnail { get; set; }

        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime InspectionDate { get; set; }

        public string BikeStatus { get; set; } = default!;
        public string ListingStatus { get; set; } = default!;
    }
}

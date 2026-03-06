using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Inspector
{
    public class BikePendingInspectionListDto
    {
        public Guid Id { get; set; }
        public Guid ListingId { get; set; }

        public string BikeName { get; set; } = default!;
        public string BikeCode { get; set; } = default!;
        public string? Thumbnail { get; set; }

        public string SellerName { get; set; } = default!;
        public string SellerPhoneNumber { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public string BikeStatus { get; set; } = default!;
    }
}

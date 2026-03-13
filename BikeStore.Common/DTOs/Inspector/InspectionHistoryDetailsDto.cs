using BikeStore.Common.DTOs.Seller.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Inspector
{
    public class InspectionHistoryDetailsDto
    {
        public Guid InspectionId { get; set; }
        public Guid BikeId { get; set; }
        public Guid ListingId { get; set; }

        public string Category { get; set; } = default!;
        public string Brand { get; set; } = default!;
        public string FrameSize { get; set; } = default!;
        public string FrameMaterial { get; set; } = default!;
        public string Paint { get; set; } = default!;
        public string Groupset { get; set; } = default!;
        public string Operating { get; set; } = default!;
        public string TireRim { get; set; } = default!;
        public string BrakeType { get; set; } = default!;
        public string Overall { get; set; } = default!;
        public decimal Price { get; set; }

        public string BikeStatus { get; set; } = default!;
        public string ListingStatus { get; set; } = default!;
        public string? ListingDescription { get; set; }

        public bool Frame { get; set; }
        public bool PaintCondition { get; set; }
        public bool Drivetrain { get; set; }
        public bool Brakes { get; set; }
        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime InspectionDate { get; set; }

        public List<SellerMediaDto> Medias { get; set; } = new();
    }
}

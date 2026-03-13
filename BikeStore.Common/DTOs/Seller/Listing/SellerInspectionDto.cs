using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Listing
{
    public class SellerInspectionDto
    {
        public bool Frame { get; set; }
        public bool PaintCondition { get; set; }
        public bool Drivetrain { get; set; }
        public bool Brakes { get; set; }

        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime InspectionDate { get; set; }
    }
}

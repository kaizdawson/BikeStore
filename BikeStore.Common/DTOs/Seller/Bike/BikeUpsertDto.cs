using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Bike
{
    public class BikeUpsertDto
    {
        [Required] public string Category { get; set; } = default!;
        [Required] public string Brand { get; set; } = default!;
        [Required] public string FrameSize { get; set; } = default!;
        [Required] public string FrameMaterial { get; set; } = default!;
        [Required] public string Paint { get; set; } = default!;
        [Required] public string Groupset { get; set; } = default!;
        [Required] public string Operating { get; set; } = default!;
        [Required] public string TireRim { get; set; } = default!;
        [Required] public string BrakeType { get; set; } = default!;
        [Required] public string Overall { get; set; } = default!;

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }
    }
}

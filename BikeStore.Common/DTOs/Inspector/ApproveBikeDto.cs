using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Inspector
{
    public class ApproveBikeDto
    {
        public bool Frame { get; set; }
        public bool PaintCondition { get; set; }
        public bool Drivetrain { get; set; }
        public bool Brakes { get; set; }

        [Range(0, 100)]
        public int Score { get; set; } = 0;

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }
}

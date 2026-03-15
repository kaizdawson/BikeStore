using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Buyer
{
    public class ReviewDto
    {
        public Guid OrderId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}

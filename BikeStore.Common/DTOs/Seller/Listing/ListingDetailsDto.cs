using BikeStore.Common.DTOs.Seller.Bike;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Listing
{
    public class ListingDetailsDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = default!;
        public string Description { get; set; } = default!;
        public string Status { get; set; } = default!;

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public BikeDto? Bike { get; set; }
    }
}

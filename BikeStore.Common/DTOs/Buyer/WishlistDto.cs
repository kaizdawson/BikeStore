using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Buyer
{
    public class WishlistDto
    {
        public Guid Id { get; set; }
        public Guid BikeId { get; set; }
        public Guid ListingId { get; set; } 
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? Brand { get; set; }    
        public string? Category { get; set; } 
        public string? BikeStatus { get; set; } 
        public string ImageUrl { get; set; } = string.Empty;
    }
}

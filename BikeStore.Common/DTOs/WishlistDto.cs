using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs
{
    public class WishlistDto
    {
        public Guid Id { get; set; } // ID của bản ghi Wishlist
        public Guid BikeId { get; set; }
        public string Title { get; set; } = default!; // Lấy từ Listing
        public decimal Price { get; set; } // Lấy từ Bike
        public string? ImageUrl { get; set; } // Lấy từ Media
    }
}

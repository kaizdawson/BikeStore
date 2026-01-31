using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Media
{
    public class SellerMediaDto
    {
        public Guid Id { get; set; }
        public Guid BikeId { get; set; }
        public string? Image { get; set; }
        public string? VideoUrl { get; set; }
    }
}

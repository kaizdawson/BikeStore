using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Buyer
{
    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid BikeId { get; set; } 
        public string BikeTitle { get; set; } = default!;
        public decimal UnitPrice { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsSelected { get; set; }
    }
}

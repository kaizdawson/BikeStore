using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs
{
    public class CartDto
    {
        public Guid Id { get; set; } 
        public Guid UserId { get; set; } 
        public decimal TotalAmount { get; set; }
    }
}

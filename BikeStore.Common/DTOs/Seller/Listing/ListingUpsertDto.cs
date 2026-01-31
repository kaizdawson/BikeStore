using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Listing
{
    public class ListingUpsertDto
    {
        [Required, MaxLength(150)]
        public string Title { get; set; } = default!;

        [Required, MaxLength(2000)]
        public string Description { get; set; } = default!;
    }
}

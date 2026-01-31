using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Seller.Media
{
    public class UploadMediaDto
    {
        public IFormFile File { get; set; } = default!;
    }
}

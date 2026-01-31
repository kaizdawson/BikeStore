using BikeStore.Common.DTOs.Seller.Media;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ICloudinaryService
    {
        Task<SellerMediaDto> UploadBikeImageAndSaveAsync(Guid sellerId, Guid bikeId, IFormFile file);
        Task<SellerMediaDto> UploadBikeVideoAndSaveAsync(Guid sellerId, Guid bikeId, IFormFile file);
    }
}

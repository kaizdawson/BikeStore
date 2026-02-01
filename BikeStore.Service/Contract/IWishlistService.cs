using BikeStore.Common.DTOs.Buyer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IWishlistService
    {
        Task<List<WishlistDto>> GetMyWishlistAsync(Guid userId);
        Task<bool> AddToWishlistAsync(Guid userId, Guid bikeId);
        Task<bool> RemoveFromWishlistAsync(Guid userId, Guid bikeId);
    }
}

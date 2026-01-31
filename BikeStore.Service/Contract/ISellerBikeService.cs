using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Bike;

namespace BikeStore.Service.Contract
{
    public interface ISellerBikeService
    {
        Task<BikeDto> CreateAsync(Guid sellerId, Guid listingId, BikeUpsertDto dto);
        Task<PagedResult<BikeDto>> GetByListingAsync(Guid sellerId, Guid listingId, int pageNumber, int pageSize);
        Task<BikeDto?> GetByIdAsync(Guid sellerId, Guid bikeId);
        Task<BikeDto?> UpdateAsync(Guid sellerId, Guid bikeId, BikeUpsertDto dto);
        Task<bool> DeleteAsync(Guid sellerId, Guid bikeId);

        Task<PagedResult<BikeDto>> GetMyBikesAsync(Guid sellerId,int pageNumber,int pageSize,Guid? listingId = null,string? status = null
);
    }
}

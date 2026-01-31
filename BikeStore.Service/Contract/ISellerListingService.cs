using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Seller.Listing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ISellerListingService
    {
        Task<ListingDto> CreateAsync(Guid sellerId, ListingUpsertDto dto);

        Task<PagedResult<ListingDto>> GetMyListingsAsync(Guid sellerId, int pageNumber, int pageSize);

        Task<ListingDto?> GetByIdAsync(Guid sellerId, Guid listingId);

        Task<ListingDto?> UpdateAsync(Guid sellerId, Guid listingId, ListingUpsertDto dto);

        Task<bool> DeleteAsync(Guid sellerId, Guid listingId);

        Task<ListingDetailsDto?> GetDetailsAsync(Guid sellerId, Guid listingId);
    }
}

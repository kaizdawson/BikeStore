using BikeStore.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IBuyerListingService
    {
        Task<List<object>> GetAllAvailableBikesAsync(int pageNumber, int pageSize);
        Task<object?> GetListingDetailByListingIdAsync(Guid listingId);
        Task<List<object>> SearchBikesByNameAsync(string name, int pageNumber, int pageSize);
        Task<List<object>> FilterBikesByTagsAsync(List<string> tags, int pageNumber, int pageSize);
    }
}
using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Repository.Models;

namespace BikeStore.Service.Contract
{
    public interface IAdminApproveService
    {
        Task<bool> ApproveListingAsync(Guid id, AdminApproveDto dto);
        Task<List<Listing>> GetPendingListingsAsync();
    }
}
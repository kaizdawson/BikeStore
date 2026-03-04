using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Repository.Models;

namespace BikeStore.Service.Contract
{
    public interface IAdminService
    {
        Task<bool> ApproveListingAsync(Guid id, AdminApproveDto dto);
        Task<List<object>> GetPendingListingsAsync();
        Task<object?> GetListingDetailAsync(Guid id);
        Task<List<object>> GetInspectingListingsAsync();
        Task<List<object>> GetActiveListingsAsync();
        Task<List<object>> GetRejectedListingsAsync();
        Task<(bool Success, string Message)> CreateInspectorAsync(SignUpDto dto);
    }
}
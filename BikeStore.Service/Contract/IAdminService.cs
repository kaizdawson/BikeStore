using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Common.Enums;
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
        Task<object> GetUsersManagerAsync(string? search, RoleEnum? role, UserStatusEnum? status, int pageNumber, int pageSize);
        Task<bool> BanUserAsync(Guid userId);
        Task<object> GetBrandStatisticsAsync(string? search, int pageNumber, int pageSize);
        Task<object> GetCategoryStatisticsAsync(string? search, int pageNumber, int pageSize);
        Task<object> GetDashboardOverviewAsync();
        Task<object> GetTransactionsForAdminAsync();
        Task<object> GetReportsForAdminAsync();
    }
}

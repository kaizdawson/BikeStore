using BikeStore.Common.DTOs;
using BikeStore.Common.DTOs.Inspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IInspectorService
    {
        Task<PagedResult<BikePendingInspectionListDto>> GetPendingBikesAsync(int pageNumber, int pageSize);
        Task<BikePendingInspectionDto?> GetPendingBikeDetailsAsync(Guid pendingBikeId);
        Task<(bool Success, string Message)> ApproveBikeAsync(Guid inspectorId, Guid bikeId, ApproveBikeDto dto);
        Task<(bool Success, string Message)> RejectBikeAsync(Guid inspectorId, Guid bikeId, string? comment);

        Task<PagedResult<InspectionHistoryListDto>> GetInspectionHistoryAsync(int pageNumber, int pageSize);
        Task<InspectionHistoryDetailsDto?> GetInspectionHistoryDetailsAsync(Guid inspectionId);
    }
}

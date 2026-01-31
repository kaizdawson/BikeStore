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
        Task<PagedResult<BikePendingInspectionDto>> GetPendingBikesAsync(int pageNumber, int pageSize);

        Task<(bool Success, string Message)> ApproveBikeAsync(Guid inspectorId, Guid bikeId, ApproveBikeDto dto);
    }
}

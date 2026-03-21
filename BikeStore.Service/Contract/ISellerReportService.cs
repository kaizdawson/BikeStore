using BikeStore.Common.DTOs.Seller.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ISellerReportService
    {
        Task<List<ReportSellerItemDto>> GetMyOrderReportsAsync();
        Task<ReportSellerItemDto> MarkProcessingAsync(Guid reportId);
        Task<ReportSellerItemDto> MarkResolvedAsync(Guid reportId);
        Task<ReportSellerItemDto> MarkRejectedAsync(Guid reportId);
    }
}

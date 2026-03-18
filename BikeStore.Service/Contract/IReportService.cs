using BikeStore.Common.DTOs.Buyer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IReportService
    {
        Task<(bool Success, string Message)> CreateReportAsync(ReportDto dto);
    }
}

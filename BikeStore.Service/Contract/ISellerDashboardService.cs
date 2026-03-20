using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ISellerDashboardService
    {
        Task<object> GetSellerDashboardAsync();
    }
}

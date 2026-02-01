using BikeStore.Common.DTOs.Buyer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ICartService
    {
        Task<CartDto> GetMyCartAsync(Guid userId);
    }
}

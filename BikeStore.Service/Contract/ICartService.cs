using BikeStore.Common.DTOs;
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

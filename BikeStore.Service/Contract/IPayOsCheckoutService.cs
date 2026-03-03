using BikeStore.Common.DTOs.PayOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IPayOsCheckoutService
    {
        Task<CheckoutResultDto> CheckoutAsync(Guid userId, Guid orderId);
    }
}

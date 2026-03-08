using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BikeStore.Common.DTOs.Admin;
using BikeStore.Repository.Models;

namespace BikeStore.Service.Contract
{
    public interface IPolicyService
    {
        Task<List<object>> GetAllPoliciesAsync();
        Task<Policy?> GetCurrentActivePolicyAsync();
        Task<bool> CreatePolicyAsync(PolicyCreateDto dto);
        Task<bool> UpdatePolicyAsync(Guid id, PolicyUpdateDto dto);
        Task<bool> DeletePolicyAsync(Guid id);
    }
}

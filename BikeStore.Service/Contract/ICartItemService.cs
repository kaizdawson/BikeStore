using BikeStore.Common.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface ICartItemService
    {
        Task<List<CartItemDto>> GetItemsByCartIdAsync(Guid cartId);
        Task<bool> AddItemAsync(Guid userId, Guid bikeId);
        Task<bool> RemoveItemAsync(Guid cartItemId);
        Task<bool> ToggleSelectionAsync(Guid cartItemId);
        Task<List<string>> ValidateCartAsync(Guid cartId);
    }
}

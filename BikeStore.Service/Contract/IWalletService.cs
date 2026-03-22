using BikeStore.Common.DTOs.Transaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Service.Contract
{
    public interface IWalletService
    {
        Task<WalletBalanceResponseDto> GetMyWalletBalanceAsync();
        Task<WithdrawalResponseDto> CreateWithdrawalAsync(CreateWithdrawalDto dto);

        Task<List<WithdrawalHistoryItemDto>> GetMyWithdrawalsAsync();

        Task<SellerFinanceResponseDto> GetMyFinanceAsync();
    }
}

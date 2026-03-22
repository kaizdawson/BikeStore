using BikeStore.Common.DTOs.Transaction;
using BikeStore.Common.Enums;
using BikeStore.Common.Helpers;
using BikeStore.Repository.Contract;
using BikeStore.Repository.Models;
using BikeStore.Service.Contract;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace BikeStore.Service.Implementation
{
    public class WalletService : IWalletService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IGenericRepository<User> _userRepo;
        private readonly IGenericRepository<Transaction> _transactionRepo;
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<User> userRepo,
            IGenericRepository<Transaction> transactionRepo,
            IUnitOfWork unitOfWork)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepo;
            _transactionRepo = transactionRepo;
            _unitOfWork = unitOfWork;
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || string.IsNullOrWhiteSpace(userIdClaim.Value))
            {
                throw new Exception("Không tìm thấy thông tin người dùng hiện tại.");
            }

            return Guid.Parse(userIdClaim.Value);
        }

        public async Task<WalletBalanceResponseDto> GetMyWalletBalanceAsync()
        {
            var userId = GetCurrentUserId();

            var user = await _userRepo.GetById(userId);
            if (user == null || user.IsDeleted)
            {
                throw new Exception("Không tìm thấy người dùng.");
            }

            return new WalletBalanceResponseDto
            {
                WalletBalance = user.WalletBalance
            };
        }

        public async Task<WithdrawalResponseDto> CreateWithdrawalAsync(CreateWithdrawalDto dto)
        {
            if (dto == null)
            {
                throw new Exception("Dữ liệu không hợp lệ.");
            }

            if (dto.Amount < 100000)
            {
                throw new Exception("Số tiền rút tối thiểu 100.000 VNĐ");
            }

            if (string.IsNullOrWhiteSpace(dto.BankName))
                throw new Exception("Tên ngân hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(dto.BankAccountNumber))
                throw new Exception("Số tài khoản không được để trống.");

            if (string.IsNullOrWhiteSpace(dto.BankAccountName))
                throw new Exception("Tên chủ tài khoản không được để trống.");

            var userId = GetCurrentUserId();

            var user = await _userRepo.GetById(userId);
            if (user == null || user.IsDeleted)
            {
                throw new Exception("Không tìm thấy người dùng.");
            }

            if (user.WalletBalance < dto.Amount)
            {
                throw new Exception("Số dư ví không đủ.");
            }

            var now = DateTimeHelper.NowVN();

            user.WalletBalance -= dto.Amount;
            user.UpdatedAt = now;
            await _userRepo.Update(user);

            var transaction = new Transaction
            {
                Id = Guid.NewGuid(),
                OrderId = null,
                UserId = userId,
                OrderCode = null,
                Status = TransactionStatusEnum.Pending,
                Description = "Withdrawal",
                Amount = dto.Amount,
                BankName = dto.BankName,
                BankAccountNumber = dto.BankAccountNumber,
                BankAccountName = dto.BankAccountName,
                PaidAt = null,
                PolicyId = null,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false
            };

            await _transactionRepo.Insert(transaction);
            await _unitOfWork.SaveChangeAsync();

            return new WithdrawalResponseDto
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                WalletBalance = user.WalletBalance,
                Status = transaction.Status,
                Description = transaction.Description,
                CreatedAt = transaction.CreatedAt
            };
        }

        public async Task<List<WithdrawalHistoryItemDto>> GetMyWithdrawalsAsync()
        {
            var userId = GetCurrentUserId();

            var user = await _userRepo.GetById(userId);
            if (user == null || user.IsDeleted)
            {
                throw new Exception("Không tìm thấy người dùng.");
            }

            var withdrawalRes = await _transactionRepo.GetAllDataByExpression(
                filter: x => x.UserId == userId
                             && x.Description == "Withdrawal"
                             && !x.IsDeleted,
                pageNumber: 1,
                pageSize: 1000,
                orderBy: x => x.CreatedAt,
                isAscending: false
            );

            var withdrawals = withdrawalRes.Items?.ToList() ?? new List<Transaction>();

            return withdrawals.Select(x => new WithdrawalHistoryItemDto
            {
                TransactionId = x.Id,
                Amount = x.Amount,
                Status = x.Status,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            }).ToList();
        }
    }
}
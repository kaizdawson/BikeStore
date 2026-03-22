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
        private readonly IGenericRepository<Order> _orderRepo;
        private readonly IGenericRepository<OrderItem> _orderItemRepo;
        private readonly IGenericRepository<Policy> _policyRepo;
        private readonly IGenericRepository<Bike> _bikeRepo;
        private readonly IGenericRepository<Listing> _listingRepo;
        private readonly IUnitOfWork _unitOfWork;

        public WalletService(
            IHttpContextAccessor httpContextAccessor,
            IGenericRepository<User> userRepo,
            IGenericRepository<Transaction> transactionRepo,
            IGenericRepository<Order> orderRepo,
            IGenericRepository<OrderItem> orderItemRepo,
            IGenericRepository<Policy> policyRepo,
            IGenericRepository<Bike> bikeRepo,
            IGenericRepository<Listing> listingRepo,
            IUnitOfWork unitOfWork)
        {
            _httpContextAccessor = httpContextAccessor;
            _userRepo = userRepo;
            _transactionRepo = transactionRepo;
            _orderRepo = orderRepo;
            _orderItemRepo = orderItemRepo;
            _policyRepo = policyRepo;
            _bikeRepo = bikeRepo;
            _listingRepo = listingRepo;
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

        public async Task<SellerFinanceResponseDto> GetMyFinanceAsync()
        {
            var sellerId = GetCurrentUserId();

            var seller = await _userRepo.GetById(sellerId);
            if (seller == null || seller.IsDeleted)
                throw new Exception("Không tìm thấy người bán.");

            
            var orderRes = await _orderRepo.GetAllDataByExpression(
                filter: o => !o.IsDeleted && o.Status == OrderStatusEnum.Completed,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: o => o.UpdatedAt ?? o.CreatedAt,
                isAscending: false
            );

            var orders = orderRes.Items?.ToList() ?? new List<Order>();
            if (!orders.Any())
            {
                return new SellerFinanceResponseDto
                {
                    AvailableBalance = seller.WalletBalance,
                    TotalRevenue = 0,
                    TotalServiceFee = 0,
                    NetProfit = 0,
                    TotalOrders = 0,
                    Orders = new List<SellerFinanceOrderItemDto>()
                };
            }

            
            var orderIds = orders.Select(o => o.Id).ToList();

            var orderItemsRes = await _orderItemRepo.GetAllDataByExpression(
                filter: oi => orderIds.Contains(oi.OrderId),
                pageNumber: 1,
                pageSize: 5000,
                orderBy: oi => oi.Id,
                isAscending: true
            );

            var orderItems = orderItemsRes.Items?.ToList() ?? new List<OrderItem>();
            if (!orderItems.Any())
            {
                return new SellerFinanceResponseDto
                {
                    AvailableBalance = seller.WalletBalance,
                    TotalRevenue = 0,
                    TotalServiceFee = 0,
                    NetProfit = 0,
                    TotalOrders = 0,
                    Orders = new List<SellerFinanceOrderItemDto>()
                };
            }

            
            var bikeIds = orderItems.Select(oi => oi.BikeId).Distinct().ToList();

            var bikesRes = await _bikeRepo.GetAllDataByExpression(
                filter: b => bikeIds.Contains(b.Id) && !b.IsDeleted,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: b => b.CreatedAt,
                isAscending: false
            );

            var bikes = bikesRes.Items?.ToList() ?? new List<Bike>();
            if (!bikes.Any())
            {
                return new SellerFinanceResponseDto
                {
                    AvailableBalance = seller.WalletBalance,
                    TotalRevenue = 0,
                    TotalServiceFee = 0,
                    NetProfit = 0,
                    TotalOrders = 0,
                    Orders = new List<SellerFinanceOrderItemDto>()
                };
            }

            
            var listingIds = bikes.Select(b => b.ListingId).Distinct().ToList();

            var listingRes = await _listingRepo.GetAllDataByExpression(
                filter: l => listingIds.Contains(l.Id) && l.UserId == sellerId && !l.IsDeleted,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: l => l.CreatedAt,
                isAscending: false
            );

            var ownedListingIds = listingRes.Items?
                .Select(l => l.Id)
                .ToHashSet() ?? new HashSet<Guid>();

            var sellerBikeMap = bikes
                .Where(b => ownedListingIds.Contains(b.ListingId))
                .ToDictionary(b => b.Id, b => b);

            if (!sellerBikeMap.Any())
            {
                return new SellerFinanceResponseDto
                {
                    AvailableBalance = seller.WalletBalance,
                    TotalRevenue = 0,
                    TotalServiceFee = 0,
                    NetProfit = 0,
                    TotalOrders = 0,
                    Orders = new List<SellerFinanceOrderItemDto>()
                };
            }

            var sellerBikeIds = sellerBikeMap.Keys.ToHashSet();
            var orderMap = orders.ToDictionary(o => o.Id, o => o);

            
            var policyRes = await _policyRepo.GetAllDataByExpression(
                filter: p => !p.IsDeleted,
                pageNumber: 1,
                pageSize: 5000,
                orderBy: p => p.AppliedDate,
                isAscending: true
            );

            var policies = policyRes.Items?.ToList() ?? new List<Policy>();

            decimal totalRevenue = 0;
            decimal totalServiceFee = 0;
            var detailRows = new List<SellerFinanceOrderItemDto>();

            
            foreach (var oi in orderItems.Where(x => sellerBikeIds.Contains(x.BikeId)))
            {
                if (!orderMap.TryGetValue(oi.OrderId, out var order))
                    continue;

                if (!sellerBikeMap.TryGetValue(oi.BikeId, out var bike))
                    continue;

                var completedDate = order.UpdatedAt ?? order.CreatedAt;

                var matchedPolicy = policies
                    .Where(p => p.AppliedDate <= completedDate)
                    .OrderByDescending(p => p.AppliedDate)
                    .FirstOrDefault();

                decimal percentFee = matchedPolicy?.PercentOfSystem ?? 0;
                decimal salePrice = oi.UnitPrice; 
                decimal serviceFee = salePrice * percentFee / 100m;
                decimal netProfit = salePrice - serviceFee;

                totalRevenue += salePrice;
                totalServiceFee += serviceFee;

                detailRows.Add(new SellerFinanceOrderItemDto
                {
                    OrderId = order.Id,
                    OrderCode = $"ORD-{order.Id.ToString()[..8].ToUpper()}",
                    ProductName = $"{bike.Brand} {bike.Category}",
                    CompletedDate = completedDate,
                    SalePrice = salePrice,
                    ServiceFee = serviceFee,
                    NetProfit = netProfit
                });
            }

            return new SellerFinanceResponseDto
            {
                AvailableBalance = seller.WalletBalance,
                TotalRevenue = totalRevenue,
                TotalServiceFee = totalServiceFee,
                NetProfit = totalRevenue - totalServiceFee,
                TotalOrders = detailRows.Select(x => x.OrderId).Distinct().Count(),
                Orders = detailRows
                    .OrderByDescending(x => x.CompletedDate)
                    .ToList()
            };
        }
    }
}
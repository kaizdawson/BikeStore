using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Transaction
{
    public class WithdrawalResponseDto
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal WalletBalance { get; set; }
        public TransactionStatusEnum Status { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

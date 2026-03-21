using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Transaction
{
    public class WithdrawalHistoryItemDto
    {
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public TransactionStatusEnum Status { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

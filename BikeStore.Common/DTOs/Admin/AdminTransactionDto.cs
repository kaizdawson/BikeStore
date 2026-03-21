using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Admin
{
    public class AdminTransactionDto
    {
        public Guid TransactionId { get; set; }
        public Guid OrderId { get; set; }
        public string CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal SystemFee { get; set; }
        public string AppliedPercent { get; set; }
        public string Status { get; set; }
    }
}

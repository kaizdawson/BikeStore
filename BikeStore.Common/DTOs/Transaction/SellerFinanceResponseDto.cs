using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Transaction
{
    public class SellerFinanceResponseDto
    {
        public decimal AvailableBalance { get; set; }

        public decimal TotalRevenue { get; set; }
        public decimal TotalServiceFee { get; set; }
        public decimal NetProfit { get; set; }
        public int TotalOrders { get; set; }

        public List<SellerFinanceOrderItemDto> Orders { get; set; } = new();
    }
}

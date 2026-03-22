using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Transaction
{
    public class SellerFinanceOrderItemDto
    {
        public Guid OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public string ProductName { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; }

        public decimal SalePrice { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal NetProfit { get; set; }
    }
}

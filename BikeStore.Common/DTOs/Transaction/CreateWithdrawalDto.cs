using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.DTOs.Transaction
{
    public class CreateWithdrawalDto
    {
        public decimal Amount { get; set; }

        public string BankName { get; set; } = default!;
        public string BankAccountNumber { get; set; } = default!;
        public string BankAccountName { get; set; } = default!;
    }
}

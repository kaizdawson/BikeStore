using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public class Review
    {
        public Guid Id { get; set; }

        public Guid TransactionId { get; set; }
        public Transaction Transaction { get; set; } = default!;

        public int Rating { get; set; }
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = BikeStore.Common.Helpers.DateTimeHelper.NowVN();
    }
}

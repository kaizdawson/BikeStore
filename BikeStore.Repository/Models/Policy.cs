using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public class Policy : BaseEntity
    {
        public Guid Id { get; set; }

        public DateTime AppliedDate { get; set; }
        public string Description { get; set; } = default!;
        public PolicyStatusEnum Status { get; set; }

        public decimal PercentOfSystem { get; set; }
        public decimal PercentOfSeller { get; set; }

        
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    }
}

using BikeStore.Common.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public class Transaction : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid? OrderId { get; set; }
        public Order? Order { get; set; } 

        public string? OrderCode { get; set; } 
        public TransactionStatusEnum Status { get; set; }
        public string? Description { get; set; }

        public decimal Amount { get; set; }

        public DateTime? PaidAt { get; set; }

        public Guid? PolicyId { get; set; }
        public Policy? Policy { get; set; }

        public Guid? UserId { get; set; }
        public User? User { get; set; } 
    }
}

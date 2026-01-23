using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public class Inspection : BaseEntity
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public User User { get; set; } = default!;

        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime InspectionDate { get; set; }

 
        public Bike? Bike { get; set; }
    }
}

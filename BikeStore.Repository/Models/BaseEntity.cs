using BikeStore.Common.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Repository.Models
{
    public abstract class BaseEntity
    {
        [Required]
        public DateTime CreatedAt { get; set; } = DateTimeHelper.NowVN();

        public DateTime? UpdatedAt { get; set; } = DateTimeHelper.NowVN();

        public bool IsDeleted { get; set; } = false;
    }
}

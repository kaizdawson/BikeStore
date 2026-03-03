using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.Enums
{
    public enum OrderStatusEnum
    {
        Pending = 1,
        Paid = 2,
        Confirmed = 3,
        Shipping = 4,
        Completed = 5,
        Cancelled = 6
    }
}

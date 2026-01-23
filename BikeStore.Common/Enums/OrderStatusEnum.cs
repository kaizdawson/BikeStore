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
        Confirmed = 2,
        Shipping = 3,
        Completed = 4,
        Cancelled = 5
    }
}

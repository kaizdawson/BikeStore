using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BikeStore.Common.Enums
{
    public enum BikeStatusEnum
    {
        PendingInspection = 1,
        Available = 2,
        Reserved = 3,
        Sold = 4,
        Disabled = 5
    }
}

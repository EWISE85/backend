using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class AssignedSmallPointItem
    {
        public Guid PostId { get; set; }
        public int SmallPointId { get; set; }
        public string SmallPointName { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }

    public class OutOfRangeSmallPointItem
    {
        public Guid PostId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AssignSmallPointResult
    {
        public int TeamId { get; set; }
        public List<AssignedSmallPointItem> Assigned { get; set; } = new();
        public List<OutOfRangeSmallPointItem> OutOfRange { get; set; } = new();
    }
}

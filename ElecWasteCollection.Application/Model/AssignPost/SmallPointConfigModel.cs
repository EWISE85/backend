using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class SmallPointConfigItem
    {
        public string SmallPointId { get; set; }
        public double RadiusKm { get; set; }
        public double MaxRoadDistanceKm { get; set; }
        public bool Active { get; set; }
    }
}

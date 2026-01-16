using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class MapboxDirectionsResponse
    {
        public List<MapboxRoute>? Routes { get; set; }
    }

    public class MapboxRoute
    {
        public double Distance { get; set; } // meters
        public double Duration { get; set; } // seconds
    }
}

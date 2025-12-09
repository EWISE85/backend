using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class CompanyWithPointsResponse
    {
        public string CompanyId { get; set; }
        public string? CompanyName { get; set; }

        public List<SmallPointDto> SmallPoints { get; set; } = new();
    }

    public class SmallPointDto
    {
        public string SmallPointId { get; set; }
        public string Name { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; }
        public double RadiusKm { get; set; }
        public double MaxRoadDistanceKm { get; set; }
        public bool Active { get; set; }
    }
}

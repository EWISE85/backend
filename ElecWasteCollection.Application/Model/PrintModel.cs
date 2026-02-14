using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class CollectionGroupDto
    {
        public string GroupCode { get; set; }
        public string Vehicle { get; set; }
        public string Collector { get; set; }
        public string GroupDate { get; set; }
        public string CollectionPoint { get; set; }
        public int TotalProduct { get; set; }
        public double TotalWeightKg { get; set; }
        public List<RouteDto> Routes { get; set; }
    }

    public class RouteDto
    {
        public int PickupOrder { get; set; }
        public string Address { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public string DimensionText { get; set; }
        public double WeightKg { get; set; }
        public string EstimatedArrival { get; set; }
    }
}

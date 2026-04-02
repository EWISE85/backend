using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    using System.Text.Json.Serialization;

    public class CompanyCapacityModel
    {
        public string CompanyId { get; set; }
        public double CompanyMaxCapacity { get; set; }
        public double CompanyCurrentCapacity { get; set; }
        public double CompanyAvailableCapacity => Math.Round(CompanyMaxCapacity - CompanyCurrentCapacity, 2);
        public double CompanyTotalAddedToday { get; set; }
        public List<SCPCapacityModel> Warehouses { get; set; } = new();
    }

    public class SCPCapacityModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double MaxCapacity { get; set; }
        public double CurrentCapacity { get; set; }
        public double AvailableCapacity { get; set; }
        public double PlannedCapacity { get; set; }
        public double AddedVolumeThisDate { get; set; }
    }
}

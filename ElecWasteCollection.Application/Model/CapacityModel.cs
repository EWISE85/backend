using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class SCPCapacityModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double MaxCapacity { get; set; }
        public double CurrentCapacity { get; set; }
        public double AvailableCapacity => MaxCapacity - CurrentCapacity;
    }

    public class CompanyCapacityModel
    {
        public string CompanyId { get; set; }
        public double CompanyAvailableCapacity => CompanyMaxCapacity - CompanyCurrentCapacity;
        public double CompanyMaxCapacity { get; set; }
        public double CompanyCurrentCapacity { get; set; }
        public List<SCPCapacityModel> Warehouses { get; set; } = new();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class RecyclerCollectionTaskDto
    {
        public string SmallCollectionPointId { get; set; }
        public string SmallCollectionName { get; set; }
        public string Address { get; set; }
        public int TotalPackage { get; set; }
        public List<PackageSimpleDto> Packages { get; set; } = new List<PackageSimpleDto>();
    }

    public class PackageSimpleDto
    {
        public string PackageId { get; set; }
        public string Status { get; set; }
        public DateTime CreateAt { get; set; }
    }

    public class RecyclerPackageFilterModel
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 10;
        public string RecyclingCompanyId { get; set; } 
        public string? Status { get; set; }
    }


}

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

    public class CompanyMetricsDto
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public DateOnly Date { get; set; }
        public int TotalOrders { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }
        // Danh sách chi tiết từng kho
        public List<SmallPointMetricsDto> SmallCollectionPoints { get; set; } = new();
    }

    public class SmallPointMetricsDto
    {
        public string PointId { get; set; }
        public string PointName { get; set; }
        public int TotalOrders { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }
    }



}

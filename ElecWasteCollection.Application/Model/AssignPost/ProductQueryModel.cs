using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class CompanyDailySummaryDto
    {
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalCompanyProducts { get; set; }
        public List<SmallPointSummaryDto> Points { get; set; } = new();
    }

    public class SmallPointSummaryDto
    {
        public string SmallCollectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalProduct { get; set; }
    }

    public class PointProductDetailDto
    {
        public Guid ProductId { get; set; }
        public string ProductName { get; set; }
        public string SenderName { get; set; }
        public string Status { get; set; }
        public double WeightKg { get; set; }
        public double VolumeM3 { get; set; }
        public string DimensionText { get; set; }
    }

    public class SmallPointCollectionMetricsDto
    {
        public string SmallPointId { get; set; }
        public string SmallPointName { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / Limit);
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }
        public List<PointProductMetricDetailDto> Products { get; set; }
    }

    public class PointProductMetricDetailDto
    {
        public Guid ProductId { get; set; }
        public Guid? SenderId { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public double WeightKg { get; set; }
        public double VolumeM3 { get; set; }
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Dimensions { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class GetCompanyProductsResponse
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string WorkDate { get; set; } = string.Empty;

        public int TotalProducts { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<SmallPointProductGroupDto> Points { get; set; } = new();
    }

    public class SmallPointProductGroupDto
    {
        public string? SmallPointId { get; set; }
        public string SmallPointName { get; set; } = string.Empty;

        public double RadiusMaxConfigKm { get; set; }
        public double MaxRoadDistanceKm { get; set; }

        public int Total { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<ProductDetailDto> Products { get; set; } = new();
    }
    public class PagedSmallPointProductGroupDto
    {
        public string SmallPointId { get; set; }
        public string SmallPointName { get; set; }
        public int Page { get; set; }
        public int Limit { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages =>
            (int)Math.Ceiling((double)TotalItems / Limit);

        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<ProductDetailDto> Products { get; set; }
            = new();
    }

    public class ProductDetailDto
    {
        public Guid ProductId { get; set; }

        //public Guid PostId { get; set; }
        public Guid SenderId { get; set; }

        public string UserName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class CollectionGroupModel
    {
        public int GroupId { get; set; }
        public string GroupCode { get; set; }
        public string ShiftId { get; set; }
        public string Vehicle { get; set; }
        public string Collector { get; set; }
        public string Date { get; set; }
        public int TotalOrders { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PreviewProductPagedResult
    {
        public string VehicleId { get; set; }
        public string PlateNumber { get; set; }
        public string VehicleType { get; set; }

        public int TotalProduct { get; set; }

        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }

        public List<object> Products { get; set; } = new();
    }
    public class PagedCompanySettingsResponse
    {
        public string CompanyId { get; set; } = null!;
        public string CompanyName { get; set; } = null!;
        public int Page { get; set; }
        public int Limit { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<PointSettingDetailDto> Points { get; set; } = new();
    }
}

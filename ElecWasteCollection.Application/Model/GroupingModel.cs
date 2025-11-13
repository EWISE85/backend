// ================= MODEL CẬP NHẬT =================
namespace ElecWasteCollection.Application.Model
{
    public class GroupingByPointRequest
    {
        public int CollectionPointId { get; set; }
        public double RadiusKm { get; set; } = 10;
        public bool SaveResult { get; set; } = false;
    }

    public class GroupingByPointResponse
    {
        public string CollectionPoint { get; set; } = "";
        public int ActiveShifts { get; set; }
        public bool SavedToDatabase { get; set; }
        public List<GroupSummary> CreatedGroups { get; set; } = new();
    }

    public class GroupSummary
    {
        public string GroupCode { get; set; } = "";
        public int ShiftId { get; set; }
        public string Vehicle { get; set; } = "";
        public string Collector { get; set; } = "";
        public int TotalPosts { get; set; }

        // ⚡ Thêm tổng khối lượng / volume đã gom
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<RouteDetail> Routes { get; set; } = new();
    }

    public class RouteDetail
    {
        public int PickupOrder { get; set; }
        public int PostId { get; set; }
        public string UserName { get; set; } = "";
        public double DistanceKm { get; set; }
        public string Address { get; set; } = "";
        public string Schedule { get; set; } = "";
        public string EstimatedArrival { get; set; } = "";

        // ⚡ Thêm chi tiết món đồ
        public double WeightKg { get; set; }
        public double VolumeM3 { get; set; }
        public string SizeTier { get; set; } = "";
    }
}

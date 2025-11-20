using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class AutoGroupRequest
    {
        public int CollectionPointId { get; set; }
        public bool SaveResult { get; set; } = true; 
    }

    public class AutoGroupResponse
    {
        public string CollectionPoint { get; set; } = "";
        public bool SavedToDatabase { get; set; }
        public List<GroupSummary> CreatedGroups { get; set; } = new();
    }

    public class GroupSummary
    {
        public int GroupId { get; set; }
        public string GroupCode { get; set; } = "";
        public int ShiftId { get; set; }
        public string Vehicle { get; set; } = "";
        public string Collector { get; set; } = "";
        public DateOnly GroupDate { get; set; }
        public int TotalPosts { get; set; }
        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<RouteDetail> Routes { get; set; } = new();
    }

    public class RouteDetail
    {
        public int PickupOrder { get; set; }
        public Guid PostId { get; set; }
        public string UserName { get; set; } = "";
        public string Address { get; set; } = "";
        public double DistanceKm { get; set; }
        public string Schedule { get; set; } = "";
        public string EstimatedArrival { get; set; } = "";
        public double WeightKg { get; set; }
        public double VolumeM3 { get; set; }
        public string SizeTier { get; set; } = "";
    }
}

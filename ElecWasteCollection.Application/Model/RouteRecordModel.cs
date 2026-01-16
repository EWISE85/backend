using System;

namespace ElecWasteCollection.Application.Model
{
    public class RouteRecordModel
    {
        public Guid CollectionRouteId { get; set; }
        public Guid PostId { get; set; }
        public int CollectionGroupId { get; set; }
        public DateOnly CollectionDate { get; set; }

        public string Status { get; set; } = "";

        public string UserName { get; set; } = "";
        public string Address { get; set; } = "";

        public string SizeTier { get; set; } = "";
        public double WeightKg { get; set; }
        public double VolumeM3 { get; set; }

        public double DistanceKm { get; set; }
        public string Schedule { get; set; } = "";
        public string EstimatedTime { get; set; } = "";

        public string EstimatedArrival { get; set; } = "";
    }
}

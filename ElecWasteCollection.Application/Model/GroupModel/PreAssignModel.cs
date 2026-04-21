using ElecWasteCollection.Domain.Entities;


namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class PreAssignRequest
    {
        public DateOnly WorkDate { get; set; }
        public List<string>? VehicleIds { get; set; }
        public string CollectionPointId { get; set; }
        public double LoadThresholdPercent { get; set; } = 80;
        public List<Guid>? ProductIds { get; set; }

    }

    public class CriticalGapSuggestion
    {
        public bool HasCriticalGap { get; set; }
        public int SuggestedVehicleCount { get; set; }
        public double TotalCriticalWeight { get; set; }
        public double TotalCriticalVolume { get; set; }
        public List<string> CriticalProductIds { get; set; } = new();
        public string Message { get; set; } = null!;
    }
    public class GetAvailableVehiclesRequest
    {
        public string PointId { get; set; } = null!;
        public DateOnly WorkDate { get; set; }
    }

    public class VehicleAvailableViewModel
    {
        public string VehicleId { get; set; } = null!;
        public string PlateNumber { get; set; } = null!;
        public string VehicleType { get; set; } = null!;
        public double CapacityKg { get; set; }
        public double CapacityM3 { get; set; }
        public string Status { get; set; } = null!;
    }

    public class PreAssignResponse
    {
        public string SmallCollectionPointId { get; set; }
        public string CollectionPoint { get; set; }
        public DateOnly WorkDate { get; set; }
        public double LoadThresholdPercent { get; set; }
        public List<PreAssignDay> Days { get; set; } = new List<PreAssignDay>();
        public List<UnAssignProductPreview> UnassignedProducts { get; set; }
        public CriticalGapSuggestion? CriticalGapSuggestion { get; set; }
    }

    public class PreAssignDay
    {
        public DateOnly WorkDate { get; set; }
        public int OriginalPostCount { get; set; }
        public double TotalWeight { get; set; }
        public double TotalVolume { get; set; }
        public SuggestedVehicle SuggestedVehicle { get; set; }
        public List<PreAssignProduct> Products { get; set; } = new List<PreAssignProduct>();
    }

    public class PreAssignProduct
    {
        public string PostId { get; set; }
        public string ProductId { get; set; }
        public string UserName { get; set; }
        public string Address { get; set; }
        public double Weight { get; set; }
        public double Volume { get; set; }
        public double DistanceKm { get; set; }
        public string DimensionText { get; set; }
        public string EstimatedArrival { get; set; }
        public string CategoryName { get; set; }
        public string BrandName { get; set; }
        public double Lat { get; set; }
        public double Lng { get; set; } 
    }
    public class VehicleBucket
    {
        public Vehicles Vehicle { get; set; } = null!;
        public double LastLat { get; set; }
        public double LastLng { get; set; }

        public double CurrentKg { get; set; }

        public double CurrentM3 { get; set; }

        public double CurrentTimeMin { get; set; }

        public double MaxKg { get; set; }

        public double MaxM3 { get; set; }

        public double MaxShiftMinutes { get; set; }

        public TimeOnly ShiftStartBase { get; set; }

        public List<PreAssignProduct> Products { get; set; } = new();
    }
    public class SuggestedVehicle
    {
        public string Id { get; set; }
        public string Plate_Number { get; set; }
        public string Vehicle_Type { get; set; }
        public double Capacity_Kg { get; set; }
        public double AllowedCapacityKg { get; set; }
        public double Capacity_M3 { get; set; }
        public double AllowedCapacityM3 { get; set; }
    }

    public class UnAssignProductPreview
    {
        public string PostId { get; set; } = string.Empty;

        public string ProductId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public double Weight { get; set; }

        public double Volume { get; set; }

        public string DimensionText { get; set; } = string.Empty;

        public string CategoryName { get; set; } = string.Empty;

        public string BrandName { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;
    }

    public class PreAssignResponseCache
    {
        public string SmallCollectionPointId { get; set; }

        public DateOnly WorkDate { get; set; }

        public DateTime CachedAt { get; set; }

        public PreAssignResponse Response { get; set; } = null!;
    }
}

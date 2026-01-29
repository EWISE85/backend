using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class PreAssignRequest
    {
        public string CollectionPointId { get; set; }
        public double LoadThresholdPercent { get; set; } = 80;
        public List<Guid>? ProductIds { get; set; }
    }

    public class PreAssignResponse
    {
        public string CollectionPoint { get; set; }
        public double LoadThresholdPercent { get; set; }
        public List<PreAssignDay> Days { get; set; } = new List<PreAssignDay>();
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
        public string DimensionText { get; set; }
        public string EstimatedArrival { get; set; } // Định dạng HH:mm
    }
    public class VehicleBucket
    {
        public Vehicles Vehicle { get; set; }
        public double CurrentTimeMin { get; set; } // Phút thứ bao nhiêu trong ca làm việc
        public double CurrentKg { get; set; }
        public double CurrentM3 { get; set; }
        public double MaxKg { get; set; }
        public double MaxM3 { get; set; }
        public double MaxShiftMinutes { get; set; } // Tổng thời gian ca làm việc (phút)
        public TimeOnly ShiftStartBase { get; set; } // Giờ bắt đầu ca (VD: 07:00)
        public List<PreAssignProduct> Products { get; set; } = new List<PreAssignProduct>();
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
}

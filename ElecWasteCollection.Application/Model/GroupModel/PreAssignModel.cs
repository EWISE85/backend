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
        public string CollectionPoint { get; set; } = "";
        public double LoadThresholdPercent { get; set; }
        public List<PreAssignDay> Days { get; set; } = new();
    }

    public class PreAssignDay
    {
        public DateOnly WorkDate { get; set; }
        public int OriginalPostCount { get; set; }
        public double TotalWeight { get; set; }
        public double TotalVolume { get; set; }

        public SuggestedVehicle? SuggestedVehicle { get; set; }
        public List<PreAssignProduct> Products { get; set; } = new();
    }

    public class PreAssignProduct
    {
        public Guid PostId { get; set; }
        public Guid ProductId { get; set; }

        public string UserName { get; set; } = "";
        public string Address { get; set; } = "";
        public double Weight { get; set; }
        public double Volume { get; set; } 
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string DimensionText { get; set; } = "";  

    }

    public class SuggestedVehicle
    {
        public string Id { get; set; }
        public string Plate_Number { get; set; } = "";
        public string Vehicle_Type { get; set; } = "";
        public double Capacity_Kg { get; set; }
        public double AllowedCapacityKg { get; set; }
        public double Capacity_M3 { get; set; }
        public double AllowedCapacityM3 { get; set; }
    }
}

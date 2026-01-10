using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ElecWasteCollection.Domain.Entities
{
    public enum VehicleStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,
        [Description("Không hoạt động")]
        KHONG_HOAT_DONG
    }
    public class Vehicles
    {
        public string VehicleId { get; set; }
        public string Plate_Number { get; set; } = null!;
        public string Vehicle_Type { get; set; } = null!;
        public int Capacity_Kg { get; set; }
        public int Capacity_M3 { get; set; }
        //public int Radius_Km { get; set; }
        public string Status { get; set; } = null!;
        public string Small_Collection_Point { get; set; }
        [JsonIgnore]
        public SmallCollectionPoints SmallCollectionPoints { get; set; } = null!;
        [JsonIgnore]
        public virtual ICollection<Shifts> Shifts { get; set; } = new List<Shifts>();
	}
}

namespace ElecWasteCollection.Domain.Entities
{
    public class Vehicles
    {
        public int Id { get; set; }
        public string Plate_Number { get; set; } = null!;
        public string Vehicle_Type { get; set; } = null!;
        public int Capacity_Kg { get; set; }
        public int Capacity_M3 { get; set; }
        public int Radius_Km { get; set; }
        public string Status { get; set; } = null!;
        public int Small_Collection_Point { get; set; }
    }
}

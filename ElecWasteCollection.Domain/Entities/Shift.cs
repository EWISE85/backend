namespace ElecWasteCollection.Domain.Entities
{
    public class Shifts
    {
        public string ShiftId { get; set; }
        public Guid CollectorId { get; set; }
        public string? Vehicle_Id { get; set; }
        public DateOnly WorkDate { get; set; }
        public DateTime Shift_Start_Time { get; set; }
        public DateTime Shift_End_Time { get; set; }
        public string Status { get; set; }

        public User Collector { get; set; } = null!;

        public Vehicles? Vehicle { get; set; } = null!;

		public virtual ICollection<CollectionGroups> CollectionGroups { get; set; } = new List<CollectionGroups>();
	}
}

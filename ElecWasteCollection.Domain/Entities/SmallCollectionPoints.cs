namespace ElecWasteCollection.Domain.Entities
{
    public enum SmallCollectionPointStatus
	{
		Active,
		Inactive,
		UnderMaintenance
	}
	public class SmallCollectionPoints
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = null!;
        public int CompanyId { get; set; }

        public string OpenTime { get; set; } = null!;

		public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
    }
}

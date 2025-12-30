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
        public string SmallCollectionPointsId { get; set; }
        public string Name { get; set; } = null!;
        public string Address { get; set; } = null!;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Status { get; set; } = null!;
        public string CompanyId { get; set; }

        public string OpenTime { get; set; } = null!;

		public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
        public double RadiusKm { get; set; }
        public double MaxRoadDistanceKm { get; set; }

        public double ServiceTimeMinutes { get; set; } = 10;
        public double AvgTravelTimeMinutes { get; set; } = 10;

        public Company CollectionCompany { get; set; } = null!;

        public virtual ICollection<User> Users { get; set; } = new List<User>();

		public virtual ICollection<Products> Products { get; set; } = new List<Products>();

		public virtual ICollection<Packages> Packages { get; set; } = new List<Packages>();

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        public virtual ICollection<Vehicles> Vehicles { get; set; } = new List<Vehicles>();
	}
}

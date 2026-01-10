using System.ComponentModel;

namespace ElecWasteCollection.Domain.Entities
{
    public enum SmallCollectionPointStatus
    {
        [Description("Hoạt động")]
        HOAT_DONG,
        [Description("Không hoạt động")]
        KHONG_HOAT_DONG,
        [Description("Bảo trì")]
        BAO_TRI
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

        public Company CollectionCompany { get; set; } = null!;
        public string? RecyclingCompanyId { get; set; }
        public Company? RecyclingCompany { get; set; }
        public virtual ICollection<User> Users { get; set; } = new List<User>();

		public virtual ICollection<Products> Products { get; set; } = new List<Products>();

		public virtual ICollection<Packages> Packages { get; set; } = new List<Packages>();

        public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        public virtual ICollection<Vehicles> Vehicles { get; set; } = new List<Vehicles>();
        public virtual ICollection<SystemConfig> CustomSettings { get; set; } = new List<SystemConfig>();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum CompanyStatus
	{
		Active,
		Inactive,
	}
    public enum CompanyType
    {
        CollectionCompany,
		RecyclingCompany,
	}
	public class Company
    {
        public string CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public string CompanyEmail { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;

        public string CompanyType { get; set; } = null!;
		public string Status { get; set; } = null!;

        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();

		public virtual ICollection<SmallCollectionPoints> SmallCollectionPoints { get; set; } = new List<SmallCollectionPoints>();

		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

        public double AssignRatio { get; set; } = 0;
    }
}

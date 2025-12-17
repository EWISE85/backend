using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum UserRole
	{
		AdminWarehouse,
		Collector,
		User,
		Admin,
		AdminCompany
	}
	public enum UserStatus
	{
		Active,
		Inactive,
		Suspended
	}
	public class User
	{
		public Guid UserId { get; set; }
		public string? Name { get; set; }

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public string? Avatar { get; set; }

		public string Role { get; set; }

		public string? SmallCollectionPointId { get; set; }

		public string? CollectionCompanyId { get; set; }

		public string Status { get; set; }

		public CollectionCompany? CollectionCompany { get; set; }

		public SmallCollectionPoints? SmallCollectionPoint { get; set; }

		public virtual ICollection<UserAddress> UserAddresses { get; set; } = new List<UserAddress>();

		public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

		public virtual ICollection<UserPoints> UserPoints { get; set; } = new List<UserPoints>();

		public virtual ICollection<Products> Products { get; set; } = new List<Products>();

		public virtual ICollection<Post> Posts { get; set; } = new List<Post>();

		public virtual ICollection<PointTransactions> PointTransactions { get; set; } = new List<PointTransactions>();

		public virtual ICollection<Shifts> Shifts { get; set; } = new List<Shifts>();

		public virtual ICollection<ForgotPassword> ForgotPasswords { get; set; } = new List<ForgotPassword>();

	}
}

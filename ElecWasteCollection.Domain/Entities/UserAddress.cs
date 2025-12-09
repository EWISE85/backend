using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class UserAddress
	{
		public Guid UserAddressId { get; set; }
		public Guid UserId { get; set; }
		public string Address { get; set; }

		public double? Iat { get; set; }

		public double? Ing { get; set; }

		public bool isDefault { get; set; }

		public User User { get; set; }
	}
}

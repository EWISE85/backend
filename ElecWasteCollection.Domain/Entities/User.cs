using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class User
	{
		public Guid UserId { get; set; }
		public string? Name { get; set; }

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public string? Address { get; set; }

		public string? Avatar { get; set; }

		public double? Iat { get; set; }

		public double? Ing { get; set; }
	}
}

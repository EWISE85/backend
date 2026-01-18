using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class UserProfileResponse
	{
		public Guid UserId { get; set; }
		public string? Name { get; set; }

		public string? Email { get; set; }

		public string? Phone { get; set; }

		public string? Avatar { get; set; }

		public string Role { get; set; }

		public double Points { get; set; }

		public string? SmallCollectionPointId { get; set; }

		public string? CollectionCompanyId { get; set; }

		public string Status { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Account
	{
		public Guid AccountId { get; set; }
		public string? Username { get; set; }
		public string? PasswordHash { get; set; }
		public Guid UserId { get; set; }

		public User User { get; set; }

	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class UserToken
	{
		public Guid UserTokenId { get; set; }

		public string Token { get; set; } = null!;
		public string AccessToken { get; set; } = null!;
		public DateTime ExpiryDate { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public Guid UserId { get; set; }

		public User User { get; set; } = null!;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class ForgotPassword
	{
		public Guid ForgotPasswordId { get; set; }

		public Guid UserId { get; set; }

		public string OTP { get; set; }

		public DateTime ExpireAt { get; set; }

		public User User { get; set; }
	}
}

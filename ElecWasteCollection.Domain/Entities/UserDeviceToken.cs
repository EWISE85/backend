using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum DevicePlatform
	{
		Android,
		IOS,
	}

	public class UserDeviceToken
    {
        public Guid UserDeviceTokenId { get; set; }

		public Guid UserId { get; set; }

		public string FCMToken { get; set; }

		public string Platform { get; set; } 

		public DateTime CreatedAt { get; set; }

		public User User { get; set; }
	}
}

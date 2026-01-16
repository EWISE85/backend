using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class UserProfileUpdateModel
    {
        public Guid UserId { get; set; }

		public string Email { get; set; }
		public string AvatarUrl { get; set; }

		public string phoneNumber { get; set; }

		public UserSettingsModel Settings { get; set; }
	}
}

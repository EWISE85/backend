using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class RegisterDeviceModel
	{
		public Guid UserId { get; set; }

		public string FcmToken { get; set; }
		public string Platform { get; set; } 
	}
}

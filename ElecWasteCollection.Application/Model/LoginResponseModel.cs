using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class LoginResponseModel
	{
		public string AccessToken { get; set; }

		public bool IsFirstLogin { get; set; }
	}
}

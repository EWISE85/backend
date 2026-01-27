using FirebaseAdmin.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IFirebaseService
    {
		Task<FirebaseToken> VerifyIdTokenAsync(string idToken);
		Task SendNotificationToDeviceAsync(string token, string title, string body, Dictionary<string, string>? data = null);

		Task<List<string>> SendMulticastAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null);
	}
}

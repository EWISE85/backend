using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService
{
	public class FirebaseService : IFirebaseService
	{
		public async Task SendMulticastAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null)
		{
			if (tokens == null || !tokens.Any()) return;

			var message = new MulticastMessage()
			{
				Tokens = tokens,
				Notification = new Notification()
				{
					Title = title,
					Body = body
				},
				Data = data,

				Android = new AndroidConfig()
				{
					Priority = Priority.High
				},

				Apns = new ApnsConfig()
				{
					Headers = new Dictionary<string, string>()
			{
				{ "apns-priority", "10" }, 
            },
					Aps = new Aps
					{
						ContentAvailable = true, 
						Sound = "default",       
					}
				}
			};

			try
			{
				var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

				if (response.FailureCount > 0)
				{
					var failedTokens = new List<string>();
					for (var i = 0; i < response.Responses.Count; i++)
					{
						if (!response.Responses[i].IsSuccess)
						{							Console.WriteLine($"[FCM Error] Token: {tokens[i]} - Error: {response.Responses[i].Exception.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FCM FATAL ERROR] {ex.Message}");
			}
		}

		public async Task SendNotificationToDeviceAsync(string token, string title, string body, Dictionary<string, string>? data = null)
		{
			var message = new Message()
			{
				Token = token,
				Notification = new Notification()
				{
					Title = title,
					Body = body
				},
				Data = data
			};

			try
			{
				await FirebaseMessaging.DefaultInstance.SendAsync(message);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"FCM Error: {ex.Message}");
				throw;
			}
		}

		public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
		{
			try
			{
				var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
				return decodedToken;
			}
			catch (Exception ex)
			{
				throw new UnauthorizedAccessException("Invalid Firebase token", ex);
			}
		}
	}
}

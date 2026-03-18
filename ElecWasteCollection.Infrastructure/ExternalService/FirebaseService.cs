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
		public async Task<List<string>> SendMulticastAsync(List<string> tokens, string title, string body, Dictionary<string, string>? data = null)
		{
			var failedTokens = new List<string>();
			if (tokens == null || !tokens.Any()) return failedTokens;

			var message = new MulticastMessage()
			{
				Tokens = tokens,
				Notification = new Notification()
				{
					Title = title,
					Body = body
				},
				Data = data,
				Android = new AndroidConfig { Priority = Priority.High },
				Apns = new ApnsConfig
				{
					Aps = new Aps { ContentAvailable = true, Sound = "default" }
				}
			};

			try
			{
				var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

				// Bỏ điều kiện if (response.FailureCount > 0) để duyệt qua tất cả các token đã gửi
				for (var i = 0; i < response.Responses.Count; i++)
				{
					if (response.Responses[i].IsSuccess)
					{
						// Thêm dòng log này để xem token nào đã gửi thành công
						Console.WriteLine($"[FCM Success] Đã gửi thành công đến Token: {tokens[i]}");
					}
					else
					{
						// Giữ nguyên logic xử lý lỗi của bạn
						var exception = response.Responses[i].Exception;
						var errorCode = exception?.MessagingErrorCode;

						if (errorCode == MessagingErrorCode.Unregistered ||
							errorCode == MessagingErrorCode.InvalidArgument)
						{
							failedTokens.Add(tokens[i]);
						}

						Console.WriteLine($"[FCM Error] Gửi thất bại đến Token: {tokens[i]} - Error: {errorCode} - Message: {exception?.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"[FCM FATAL ERROR] {ex.Message}");
			}

			return failedTokens;
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

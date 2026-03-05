using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ElecWasteCollection.API.Hubs
{
	[Authorize]
	public class WebNotificationHub : Hub
	{
		private readonly ILogger<WebNotificationHub> _logger;

		public WebNotificationHub(ILogger<WebNotificationHub> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			string? userId = Context.UserIdentifier;
			string connectionId = Context.ConnectionId;

			if (string.IsNullOrEmpty(userId))
			{
				_logger.LogError($"[SignalR Connect] ConnectionId: {connectionId} - LỖI: Không lấy được UserID (NULL).");

				var user = Context.User;
				if (user?.Claims != null)
				{
					foreach (var claim in user.Claims)
					{
						_logger.LogWarning($"-- Found Claim: Type={claim.Type}, Value={claim.Value}");
					}
				}
				else
				{
					_logger.LogError("-- Không tìm thấy bất kỳ Claim nào (User chưa Authenticated).");
				}
			}
			else
			{
				_logger.LogInformation($"[SignalR Connect] ConnectionId: {connectionId} - UserID: {userId} - Đã kết nối.");

				await Groups.AddToGroupAsync(connectionId, userId);
				_logger.LogInformation($"[SignalR Group] Đã thêm Connection {connectionId} vào Group {userId}");
			}

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			string? userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				_logger.LogInformation($"[SignalR Disconnect] User {userId} đã ngắt kết nối.");
			}

			await base.OnDisconnectedAsync(exception);
		}
	}
}
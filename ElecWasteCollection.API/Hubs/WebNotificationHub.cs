using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ElecWasteCollection.API.Hubs
{
	// QUAN TRỌNG: Phải có [Authorize] thì Context.User mới có dữ liệu
	[Authorize]
	public class WebNotificationHub : Hub
	{
		private readonly ILogger<WebNotificationHub> _logger;

		// Inject Logger vào Constructor
		public WebNotificationHub(ILogger<WebNotificationHub> logger)
		{
			_logger = logger;
		}

		public override async Task OnConnectedAsync()
		{
			// Lấy UserID
			string? userId = Context.UserIdentifier;
			string connectionId = Context.ConnectionId;

			// --- ĐOẠN LOG DEBUG ---
			if (string.IsNullOrEmpty(userId))
			{
				_logger.LogError($"[SignalR Connect] ConnectionId: {connectionId} - LỖI: Không lấy được UserID (NULL).");

				// In ra xem user này có claim gì không (để xem token có vào được không)
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

				// Add vào Group
				await Groups.AddToGroupAsync(connectionId, userId);
				_logger.LogInformation($"[SignalR Group] Đã thêm Connection {connectionId} vào Group {userId}");
			}
			// -----------------------

			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			string? userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				// SignalR tự remove connection khỏi group khi disconnect, nhưng log ra để biết
				_logger.LogInformation($"[SignalR Disconnect] User {userId} đã ngắt kết nối.");
			}

			await base.OnDisconnectedAsync(exception);
		}
	}
}
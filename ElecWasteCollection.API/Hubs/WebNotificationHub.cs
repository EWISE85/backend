using Microsoft.AspNetCore.SignalR;

namespace ElecWasteCollection.API.Hubs
{
	public class WebNotificationHub : Hub
	{
		// Ghi đè hàm này để xử lý ngay khi có kết nối mới
		public override async Task OnConnectedAsync()
		{
			// Lấy UserID từ Token (SignalR tự map từ ClaimTypes.NameIdentifier)
			string? userId = Context.UserIdentifier;

			if (!string.IsNullOrEmpty(userId))
			{
				// QUAN TRỌNG: Add connection hiện tại vào Group có tên là userId
				await Groups.AddToGroupAsync(Context.ConnectionId, userId);
			}

			await base.OnConnectedAsync();
		}
	}
}
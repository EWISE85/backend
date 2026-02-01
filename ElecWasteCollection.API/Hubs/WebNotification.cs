using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.SignalR;

namespace ElecWasteCollection.API.Hubs
{
	public class WebNotification : IWebNotificationService
	{
		private readonly IHubContext<WebNotificationHub> _hubContext;

		public WebNotification(IHubContext<WebNotificationHub> hubContext)
		{
			_hubContext = hubContext;
		}

		public async Task SendNotificationAsync(string userId, string title, string message, string type = "info", object? data = null)
		{
			await _hubContext.Clients.Group(userId).SendAsync("ReceiveNotification", new
			{
				Title = title,
				Message = message,
				Type = type,
				Data = data,
				Timestamp = DateTime.UtcNow
			});
		}
	}
}
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.SignalR;

namespace ElecWasteCollection.Infrastructure.Hubs
{
	public class CallHub : Hub
	{
		private readonly IConnectionManager _connectionManager;

		public CallHub(IConnectionManager connectionManager)
		{
			_connectionManager = connectionManager;
		}

		public async Task RegisterUser(string userIdString) 
		{
			if (Guid.TryParse(userIdString, out Guid userId)) 
			{
				_connectionManager.AddConnection(userId, Context.ConnectionId);

				await Clients.Caller.SendAsync("Registered", "Online status updated");
			}
		}
		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			_connectionManager.RemoveConnection(Context.ConnectionId);

			await base.OnDisconnectedAsync(exception);
		}
	}
}

using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ElecWasteCollection.Infrastructure.Hubs;
namespace ElecWasteCollection.Infrastructure.ExternalService.CallApp
{
	public class SignalRNotificationService : ICallNotificationService
	{
		private readonly IHubContext<CallHub> _hubContext;
		private readonly IConnectionManager _connectionManager;

		public SignalRNotificationService(IHubContext<CallHub> hubContext, IConnectionManager connectionManager)
		{
			_hubContext = hubContext;
			_connectionManager = connectionManager;
		}
		public async Task SendIncomingCallAsync(Guid calleeId, object callData)
		{
			var connectionId = _connectionManager.GetConnectionId(calleeId);
			if (!string.IsNullOrEmpty(connectionId))
			{
				await _hubContext.Clients.Client(connectionId).SendAsync("IncomingCall", callData);
			}
		}
		public async Task SendCallEndedAsync(Guid targetUserId, string callId)
		{
			var connectionId = _connectionManager.GetConnectionId(targetUserId);
			if (!string.IsNullOrEmpty(connectionId))
			{
				await _hubContext.Clients.Client(connectionId).SendAsync("CallEnded", new { callId });
			}
		}
	}
}

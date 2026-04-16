//using DocumentFormat.OpenXml.Vml.Office;
//using ElecWasteCollection.Application.IServices;
//using Microsoft.AspNetCore.SignalR;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ElecWasteCollection.Application.Services
//{
//	public class SignalRNotificationService : ICallNotificationService
//	{
//		private readonly IHubContext<CallHub> _hubContext;
//		private readonly IConnectionManager _connectionManager;

//		public SignalRNotificationService(IHubContext<CallHub> hubContext, IConnectionManager connectionManager)
//		{
//			_hubContext = hubContext;
//			_connectionManager = connectionManager;
//		}

//		public async Task SendIncomingCallAsync(Guid calleeId, object callData)
//		{
//			var connectionId = _connectionManager.GetConnectionId(calleeId);
//			if (!string.IsNullOrEmpty(connectionId))
//			{
//				// Bắn tín hiệu đến đúng ConnectionId của người nhận
//				await _hubContext.Clients.Client(connectionId).SendAsync("IncomingCall", callData);
//			}
//		}
//	}
//}

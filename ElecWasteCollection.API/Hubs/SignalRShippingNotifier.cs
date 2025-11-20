using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using Microsoft.AspNetCore.SignalR;

namespace ElecWasteCollection.API.Hubs
{
	public class SignalRShippingNotifier : IShippingNotifierService
	{
		private readonly IHubContext<ShippingHub> _hubContext;
		public SignalRShippingNotifier(IHubContext<ShippingHub> hubContext)
		{
			_hubContext = hubContext;
		}
		public async Task NotifyShipperOfConfirmation(string shipperId, Guid collectionRouteId, string status)
		{
			string shipperGroup = $"Shipper_{shipperId}";

			await _hubContext.Clients
				.Group(shipperGroup)
				.SendAsync("ReceiveConfirmation", collectionRouteId, status);
		}

		public async Task NotifyUserOfCollectorArrival(Guid ProductId)
		{
			await _hubContext.Clients.Group(ProductId.ToString())
				.SendAsync("ShowConfirmButton", new
				{
					Message = "Nhân viên thu gom đã đến. Vui lòng xác nhận!",
					ProductId = ProductId
				});
		}
	}
}

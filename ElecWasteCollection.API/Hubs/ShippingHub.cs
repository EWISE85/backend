using Microsoft.AspNetCore.SignalR;

namespace ElecWasteCollection.API.Hubs
{
	public class ShippingHub : Hub
	{
		public async Task JoinShipperGroup(string shipperId)
		{
			string groupName = $"Shipper_{shipperId}";
			await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
		}

		public async Task JoinRouteGroup(string productId)
		{
			// Đưa ConnectionId hiện tại vào group có tên là ID của sản phẩm
			await Groups.AddToGroupAsync(Context.ConnectionId, productId);
		}

	}
}

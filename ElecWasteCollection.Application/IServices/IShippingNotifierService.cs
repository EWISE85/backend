using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IShippingNotifierService
	{
		Task NotifyShipperOfConfirmation(string shipperId, Guid collectionRouteId, string status);

		Task NotifyUserOfCollectorArrival(Guid ProductId);

	}
}

using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface INotificationService
	{
		Task NotifyCustomerArrivalAsync(Guid productId);
		Task NotifyCustomerCallAsync(Guid routeId, Guid userId);

		Task<List<NotificationModel>> GetNotificationByUserIdAsync(Guid userId);	

		Task<bool> ReadNotificationAsync(List<Guid> notificationIds);

		Task SendNotificationToUser(SendNotificationToUserModel sendNotificationToUserModel);

		Task<List<NotificationModel>> GetNotificationTypeEvent();
	}
}

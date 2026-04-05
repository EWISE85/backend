using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
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

		Task ProcessApprovalNotificationsAsync(List<Post> post);
		Task ProcessRejectionNotificationsAsync(List<Post> rejectedPosts, string reason);

		Task NotifyCustomerCO2SavedAsync(Guid userId, double co2Saved, string oldRankName = null, string newRankName = null);

		Task SendNotificationForUserWhenReportAnswerd(Guid userId);
		Task NotifyScheduleConfirmedAsync(Dictionary<Guid, (DateOnly Date, string Time)> userSchedules);
	}
}

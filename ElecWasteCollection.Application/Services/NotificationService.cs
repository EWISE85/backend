using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class NotificationService : INotificationService
	{
		private readonly IFirebaseService _firebaseService;
		private readonly IUnitOfWork _unitOfWork;
		public NotificationService(IFirebaseService firebaseService, IUnitOfWork unitOfWork)
		{
			_firebaseService = firebaseService;
			_unitOfWork = unitOfWork;
		}

		public async Task<List<NotificationModel>> GetNotificationByUserIdAsync(Guid userId)
		{
			var notifications = await _unitOfWork.Notifications.GetsAsync(n => n.UserId == userId);
			if (notifications == null || !notifications.Any())
			{
				return new List<NotificationModel>();
			}
			var result = notifications.Select(n => new NotificationModel
			{
				NotificationId = n.NotificationId,
				Title = n.Title,
				Message = n.Body,
				IsRead = n.IsRead,
				CreatedAt = n.CreatedAt,
				UserId = n.UserId
			}).OrderByDescending(n => n.CreatedAt).ToList();
			return result;
		}

		public async Task NotifyCustomerArrivalAsync(Guid productId)
		{
			var product = await _unitOfWork.Products.GetAsync(p => p.ProductId == productId, includeProperties: "Category");
			if (product == null) throw new AppException("Không tìm thấy sản phẩm", 404);
			string productName = product.Category?.Name ?? "sản phẩm";
			string title = "Shipper sắp đến!";
			string body = $"Tài xế đang ở rất gần để thu gom '{productName}'. Vui lòng chuẩn bị."; 
			var dataPayload = new Dictionary<string, string>
			{
				{ "type", "SHIPPER_ARRIVAL" },
				{ "productId", product.ProductId.ToString() },
			};
			var userTokens = await _unitOfWork.UserDeviceTokens.GetsAsync(udt => udt.UserId == product.UserId);

			if (userTokens != null && userTokens.Any())
			{
				var tokens = userTokens.Select(d => d.FCMToken).Distinct().ToList();
				await _firebaseService.SendMulticastAsync(tokens, title, body, dataPayload);
			}
			var notification = new Notifications
			{
				NotificationId = Guid.NewGuid(),
				UserId = product.UserId,
				Title = title,
				Body = body,
				IsRead = false,
				CreatedAt = DateTime.UtcNow,
			};

			await _unitOfWork.Notifications.AddAsync(notification);
			await _unitOfWork.SaveAsync();
		}

		public async Task<bool> ReadNotificationAsync(List<Guid> notificationIds)
		{
			var notifications = await _unitOfWork.Notifications.GetsAsync(n => notificationIds.Contains(n.NotificationId));
			if (notifications == null || !notifications.Any()) throw new AppException("Không tìm thấy thông báo", 404);
			foreach (var notification in notifications)
			{
				notification.IsRead = true;
				_unitOfWork.Notifications.Update(notification);
			}
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}

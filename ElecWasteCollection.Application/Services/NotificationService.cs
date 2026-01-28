using DocumentFormat.OpenXml.Wordprocessing;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
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
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly IUnitOfWork _unitOfWork;
		public NotificationService(IFirebaseService firebaseService, IUnitOfWork unitOfWork, IServiceScopeFactory serviceScopeFactory)
		{
			_firebaseService = firebaseService;
			_unitOfWork = unitOfWork;
			_scopeFactory = serviceScopeFactory;
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

		public async Task NotifyCustomerCallAsync(Guid routeId, Guid userId)
		{
			var userTokens = await _unitOfWork.UserDeviceTokens.GetsAsync(udt => udt.UserId == userId);
			string title = "Cuộc gọi từ tài xế!";
			string body = "Tài xế đang cố gắng liên lạc với bạn. Vui lòng kiểm tra cuộc gọi.";
			var dataPayload = new Dictionary<string, string>
			{
				{ "type", "COLLECTOR_CALL" },
				{ "routeId", routeId.ToString() },
			};
			if (userTokens != null && userTokens.Any())
			{
				var tokens = userTokens.Select(d => d.FCMToken).Distinct().ToList();
				await _firebaseService.SendMulticastAsync(tokens, title, body, dataPayload);
			}
			var notification = new Notifications
			{
				NotificationId = Guid.NewGuid(),
				UserId = userId,
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

		public async Task SendNotificationToUser(SendNotificationToUserModel model)
		{
			var userBatchSize = 500;
			var userIdBatches = model.UserIds.Chunk(userBatchSize);
			foreach (var batch in userIdBatches)
			{
				foreach (var userId in batch)
				{
					var notification = new Notifications
					{
						NotificationId = Guid.NewGuid(),
						UserId = userId,
						Title = model.Title,
						Body = model.Message,
						CreatedAt = DateTime.UtcNow,
						IsRead = false
					};
					await _unitOfWork.Notifications.AddAsync(notification);
				}
				await _unitOfWork.SaveAsync();
			}
			var allTokens = new List<string>();
			foreach (var batch in model.UserIds.Chunk(1000))
			{
				var tokens = await _unitOfWork.UserDeviceTokens.GetsAsync(udt => batch.Contains(udt.UserId));
				if (tokens != null)
				{
					allTokens.AddRange(tokens.Select(t => t.FCMToken));
				}
			}
			if (allTokens.Any())
			{
				_ = Task.Run(async () =>
				{
					// Tạo một Scope mới vì đây là luồng độc lập
					using (var scope = _scopeFactory.CreateScope())
					{
						var scopedUnitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
						var scopedFirebase = scope.ServiceProvider.GetRequiredService<IFirebaseService>();

						try
						{
							var fcmBatches = allTokens.Distinct().Chunk(500);
							foreach (var fcmBatch in fcmBatches)
							{
								// Lấy danh sách token bị lỗi từ Firebase
								var failedTokens = await scopedFirebase.SendMulticastAsync(fcmBatch.ToList(), model.Title, model.Message);

								if (failedTokens.Any())
								{
									var entitiesToDelete = await scopedUnitOfWork.UserDeviceTokens.GetsAsync(t => failedTokens.Contains(t.FCMToken));
									foreach (var entity in entitiesToDelete)
									{
										scopedUnitOfWork.UserDeviceTokens.Delete(entity);
									}
									await scopedUnitOfWork.SaveAsync();
									Console.WriteLine($"[Cleanup] Đã xóa {failedTokens.Count} token hết hạn.");
								}
							}
						}
						catch (Exception ex)
						{
							Console.WriteLine($"[Background Cleanup Error]: {ex.Message}");
						}
					}
				});
			}
		}
	}
}

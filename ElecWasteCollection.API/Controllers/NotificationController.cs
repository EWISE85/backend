using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/notifications")]
	[ApiController]
	public class NotificationController : ControllerBase
	{
		private readonly IUserDeviceTokenService _userDeviceTokenService;
		private readonly INotificationService _notificationService;
		public NotificationController(IUserDeviceTokenService userDeviceTokenService, INotificationService notificationService)
		{
			_userDeviceTokenService = userDeviceTokenService;
			_notificationService = notificationService;
		}
		[HttpPost("register-device")]
		public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest registerDeviceModel)
		{
			var model = new RegisterDeviceModel
			{
				UserId = registerDeviceModel.UserId,
				FcmToken = registerDeviceModel.FcmToken,
				Platform = registerDeviceModel.Platform
			};
			var result = await _userDeviceTokenService.RegisterDeviceAsync(model);
			if (result)
			{
				return Ok(new { message = "Device registered successfully." });
			}
			else
			{
				return BadRequest(new { message = "Failed to register device." });
			}
		}
		[HttpPost("notify-arrival")]
		public async Task<IActionResult> NotifyArrival([FromBody] NotifyArrivalRequest request)
		{
			await _notificationService.NotifyCustomerArrivalAsync(request.ProductId);
			return Ok(new { message = "Notification sent to customer" });
		}
		[HttpGet("user/{userId}")]
		public async Task<IActionResult> GetNotificationsByUserId([FromRoute] Guid userId)
		{
			var notifications = await _notificationService.GetNotificationByUserIdAsync(userId);
			return Ok(notifications);
		}
		[HttpPut("read")]
		public async Task<IActionResult> ReadNotification([FromBody] ReadNotificationRequest request)
		{
			var result = await _notificationService.ReadNotificationAsync(request.NotificationIds);
			if (result)
			{
				return Ok(new { message = "Notification marked as read." });
			}
			else
			{
				return BadRequest(new { message = "Failed to mark notification as read." });
			}
		}

		[HttpPost("send-to-users")]
		public async Task<IActionResult> SendNotificationToUser([FromBody] SendNotificationToUserRequest request)
		{
			var model = new SendNotificationToUserModel
			{
				UserIds = request.UserIds,
				Title = request.Title,
				Message = request.Message
			};
			await _notificationService.SendNotificationToUser(model);
			return Ok(new { message = "Notification sent to users." });
		}
		[HttpPost("notify-call")]
		public async Task<IActionResult> NotifyCall([FromBody] NotifyCallRequest request)
		{
			await _notificationService.NotifyCustomerCallAsync(request.RouteId, request.UserId);
			return Ok(new { message = "Notification sent to customer" });
		}
		[HttpGet("event")]
		public async Task<IActionResult> GetEventNotifications()
		{
			var notifications = await _notificationService.GetNotificationTypeEvent();
			return Ok(notifications);
		}
	}
}

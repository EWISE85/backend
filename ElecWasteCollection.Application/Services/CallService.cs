using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
	public class CallService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IConnectionManager _connectionManager;
		private readonly IApnsService _apnsService;
		private readonly ICallNotificationService _notificationService;

		public CallService(
			IUnitOfWork unitOfWork,
			IConnectionManager connectionManager,
			IApnsService apnsService,
			ICallNotificationService notificationService)
		{
			_unitOfWork = unitOfWork;
			_connectionManager = connectionManager;
			_apnsService = apnsService;
			_notificationService = notificationService;
		}

		public async Task<string> InitiateCallAsync(Guid callerId, string callerName, Guid calleeId, string callId, string roomId)
		{
			// 1. Kiểm tra trạng thái Online trong Redis
			var isOnline = _connectionManager.IsUserOnline(calleeId);

			if (isOnline)
			{
				var callData = new
				{
					callerId = callerId,
					callerName = callerName,
					callId = callId,
					roomId = roomId,
					type = "incoming_call"
				};

				await _notificationService.SendIncomingCallAsync(calleeId, callData);

				return "zego_handled";
			}

			var device = await _unitOfWork.UserDeviceTokens.GetAsync(
				x => x.UserId == calleeId && x.Platform.ToLower() == DevicePlatform.IOS.ToString().ToLower());

			if (device == null || string.IsNullOrEmpty(device.VoipToken))
			{
				throw new Exception("Người nhận không trực tuyến và chưa đăng ký VoIP Token.");
			}

			var payload = new
			{
				aps = new Dictionary<string, int>
				{
					{ "content-available", 1 }
				},
				call_id = callId,
				caller_id = callerId.ToString(),
				caller_name = callerName,
				room_id = roomId,
				type = "video_call"
			};
			var isSent = await _apnsService.SendVoipPushAsync(device.VoipToken, payload);
			return isSent ? "voip_pushed" : "push_failed";
		}
		public async Task EndCallAsync(Guid partnerId, string callId)
		{
			await _notificationService.SendCallEndedAsync(partnerId, callId);
		}
	}
}
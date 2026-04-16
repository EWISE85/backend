using ElecWasteCollection.API.DTOs.Request.Call;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/call")]
	[ApiController]
	public class CallController : ControllerBase
	{
		private readonly CallService _callService;
		private readonly IConnectionManager _manager;

		public CallController(CallService callService, IConnectionManager connectionManager)
		{
			_callService = callService;
			_manager = connectionManager;
		}

		[HttpPost("initiate")]
		public async Task<IActionResult> InitiateCall([FromBody] InitiateCallRequest req)
		{
			try
			{
				var action = await _callService.InitiateCallAsync(
					req.CallerId,
					req.CallerName,
					req.CalleeId,
					req.CallId,
					req.RoomId);

				return Ok(new
				{
					success = true,
					action = action,
					message = action == "zego_handled"
						? "User is online, call via Zego directly."
						: "User is offline, VoIP push notification sent."
				});
			}
			catch (Exception ex)
			{
				// Trả về lỗi nếu không tìm thấy token hoặc lỗi server
				return BadRequest(new { success = false, message = ex.Message });
			}
		}
		[HttpPost("end")]
		public async Task<IActionResult> EndCall([FromBody] EndCallRequest req)
		{
			// App truyền lên: partnerId (người bên kia) và callId
			await _callService.EndCallAsync(req.PartnerId, req.CallId);
			return Ok(new { success = true, message = "Call termination signal sent." });
		}
		[HttpGet("status/{userId}")]
		public IActionResult GetUserStatus(Guid userId)
		{
			var isOnline = _manager.IsUserOnline(userId);

			return Ok(new
			{
				userId = userId,
				isOnline = isOnline,
				status = isOnline ? "Online" : "Offline"
			});
		}
	}
}

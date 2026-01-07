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
		public NotificationController(IUserDeviceTokenService userDeviceTokenService)
		{
			_userDeviceTokenService = userDeviceTokenService;
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
	}
}

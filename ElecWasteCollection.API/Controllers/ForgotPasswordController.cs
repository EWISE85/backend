using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/forgot-password")]
	[ApiController]
	public class ForgotPasswordController : ControllerBase
	{
		private readonly IForgotPasswordService _forgotPasswordService;
		private readonly IAccountService _accountService;
		public ForgotPasswordController(IForgotPasswordService forgotPasswordService, IAccountService accountService)
		{
			_forgotPasswordService = forgotPasswordService;
			_accountService = accountService;
		}
		[HttpPost("save-otp")]
		public async Task<IActionResult> SaveOTP([FromBody] CreateForgotPasswordRequest forgotPassword)
		{
			var model = new CreateForgotPasswordModel
			{
				Email = forgotPassword.Email
			};
			var result = await _forgotPasswordService.SaveOTP(model);
			if (result)
			{
				return Ok(new { Message = "OTP saved successfully." });
			}
			return BadRequest(new { Message = "Failed to save OTP." });
		}
		[HttpPost("check-otp")]
		public async Task<IActionResult> CheckOTP([FromBody] CheckOTPRequest checkOTP)
		{
			var result = await _forgotPasswordService.CheckOTP(checkOTP.Email, checkOTP.OTP);
			if (result)
			{
				return Ok(new { Message = "OTP is valid." });
			}
			return BadRequest(new { Message = "Invalid OTP." });
		}

		[HttpPost("re-pass")]
		public async Task<IActionResult> RePassword([FromBody] RePasswordRequest request)
		{
			
			var result = await _accountService.ChangePassword(request.Email, request.NewPassword, request.ConfirmNewPassword);
			if (result)
			{
				return Ok(new { message = "Đổi mật khẩu thành công" });
			}
			else
			{
				return BadRequest(new { message = "Đổi mật khẩu thất bại" });
			}
		}


	}
}

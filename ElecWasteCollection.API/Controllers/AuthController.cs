using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model.Tokens;
using Google.Apis.Auth.OAuth2.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/auth")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IAccountService _accountService;
		public AuthController(IAccountService accountService)
		{
			_accountService = accountService;
		}
		[HttpPost("login-google")]
		public async Task<IActionResult> LoginWithGoogle([FromBody] LoginGGRequest request)
		{
			var response = await _accountService.LoginWithGoogleAsync(request.Token);
			return Ok(new { token = response });
		}
		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest request)
		{
			var response = await _accountService.Login(request.Username, request.Password);
			return Ok(response);
		}
		[HttpPost("login-apple")]
		public async Task<IActionResult> LoginWithApple([FromBody] AppleLoginRequest request)
		{
			var response = await _accountService.LoginWithAppleAsync(request.IdentityToken, request.FirstName, request.LastName);
			return Ok(new { token = response });
		}
		[HttpPost("refresh-token")]
		[AllowAnonymous] 
		public async Task<IActionResult> RefreshToken([FromBody] RefreshTokensRequest request)
		{
			var model = new RefreshTokenModel
			{
				AccessToken = request.AccessToken,
				RefreshToken = request.RefreshToken
			};
			if (model == null)
			{
				return BadRequest("Dữ liệu yêu cầu không hợp lệ.");
			}

			var result = await _accountService.RefreshTokenAsync(model);

			if (result == null)
			{
				return Unauthorized(new
				{
					message = "Phiên đăng nhập đã hết hạn hoặc tài khoản đã đăng nhập ở thiết bị khác."
				});
			}

			return Ok(result);
		}
		[HttpPost("logout")]
		public async Task<IActionResult> Logout([FromBody] string userId)
		{
			var result = await _accountService.LogoutAsync(Guid.Parse(userId));
			if (!result)
			{
				return BadRequest("Đăng xuất thất bại.");
			}
			return Ok(new
			{
				message = "Đăng xuất thành công."
			});
		}

	}
}

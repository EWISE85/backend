using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
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


	}
}

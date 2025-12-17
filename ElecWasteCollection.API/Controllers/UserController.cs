using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/users")]
	[ApiController]
	public class UserController : ControllerBase
	{
		private readonly IUserService _userService;
		public UserController(IUserService userService)
		{
			_userService = userService;
		}
		[HttpGet]
		public async Task<IActionResult> GetAllUsers()
		{
			var users = await _userService.GetAll();
			return Ok(users);
		}
		//[HttpPut("{id}")]
		//public IActionResult UpdateUser([FromBody] UpdateUserRequest updateUserRequest, [FromRoute] Guid id)
		//{

		//	_userService.UpdateUser(updateUserRequest.Iat, updateUserRequest.Ing, id);
		//	return Ok(new { message = $"User {id} updated successfully." });
		//}
		[HttpGet("profile")]
		public async Task<IActionResult> GetProfile()
		{
			var accountEmail = User.FindFirst(ClaimTypes.Email)?.Value;
			if (accountEmail == null)
			{
				return Unauthorized(new
				{
					message = "Chưa login"
				});
			}
			var user = await _userService.Profile(accountEmail);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}
			return Ok(user);
		}
		[HttpGet("{id}")]
		public async Task<IActionResult> GetUserById([FromRoute] Guid id)
		{
			var user = await _userService.GetById(id);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}
			return Ok(user);
		}

		[HttpGet("phone/{phone}")]
		public async Task<IActionResult> GetUserByPhone([FromRoute] string phone)
		{
			var user = await _userService.GetByPhone(phone);
			if (user == null)
			{
				return NotFound(new { message = "User not found." });
			}
			return Ok(user);
		}
	}
}

using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
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
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest updateUserRequest, [FromRoute] Guid id)
		{
			var settingModel = new UserSettingsModel
			{
				ShowMap = updateUserRequest.Settings.IsShowMap,
			};
			var model = new UserProfileUpdateModel
			{
				UserId = id,
				Email = updateUserRequest.Email,
				AvatarUrl = updateUserRequest.AvatarUrl,
				phoneNumber = updateUserRequest.PhoneNumber,
				Settings = settingModel
			};
			var result = await	 _userService.UpdateProfile(model);
			return Ok(new { message = $"User {id} updated successfully." });
		}
		[HttpGet("profile")]
		public async Task<IActionResult> GetProfile()
		{
			var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			if (string.IsNullOrEmpty(userIdStr))
			{
				return Unauthorized(new { message = "Token không hợp lệ (Thiếu ID)." });
			}


			var user = await _userService.Profile(Guid.Parse(userIdStr));

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
		[HttpDelete("{userId}")]
		public async Task<IActionResult> DeleteUser([FromRoute] Guid userId)
		{
			var result = await _userService.DeleteUser(userId);
			if (!result)
			{
				return BadRequest(new { message = "Failed to delete user." });
			}
			return Ok(new { message = "User deleted successfully." });
		}
	}
}

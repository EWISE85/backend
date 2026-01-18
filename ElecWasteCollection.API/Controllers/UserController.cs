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
			var model = new UserProfileUpdateModel
			{
				UserId = id,
				Email = updateUserRequest.Email,
				AvatarUrl = updateUserRequest.AvatarUrl,
				phoneNumber = updateUserRequest.PhoneNumber,
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

		[HttpGet("infomation/{infomation}")]
		public async Task<IActionResult> GetUserByPhone([FromRoute] string infomation)
		{
			var user = await _userService.GetByEmailOrPhone(infomation);
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
		[HttpGet("email")]
		public async Task<IActionResult> GetUserByEmail([FromQuery] string email)
		{
			var users = await _userService.GetByEmail(email);
			if (users == null || users.Count == 0)
			{
				return NotFound(new { message = "User not found." });
			}
			return Ok(users);
		}

		[HttpDelete("ban/{userId}")]
		public async Task<IActionResult> BanUser([FromRoute] Guid userId)
		{
			var result = await _userService.BanUser(userId);
			if (!result)
			{
				return BadRequest(new { message = "Failed to ban user." });
			}
			return Ok(new { message = "User banned successfully." });
		}
		[HttpGet("filter")]
		public async Task<IActionResult> AdminFilterUser([FromQuery] AdminFilterUserRequest request)
		{
			var model = new AdminFilterUserModel
			{
				Page = request.Page,
				Limit = request.Limit,
				FromDate = request.FromDate,
				ToDate = request.ToDate,
				Email = request.Email,
				Status = request.Status
			};
			var users = await _userService.AdminFilterUser(model);
			return Ok(users);
		}

	}
}

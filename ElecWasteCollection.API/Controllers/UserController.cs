using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
		public IActionResult GetAllUsers()
		{
			var users = _userService.GetAll();
			return Ok(users);
		}
		[HttpPut("{id}")]
		public IActionResult UpdateUser([FromBody] UpdateUserRequest updateUserRequest, [FromRoute] Guid id)
		{

			_userService.UpdateUser(updateUserRequest.Iat, updateUserRequest.Ing, id);
			return Ok(new { message = $"User {id} updated successfully." });
		}
	}
}

using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/user-address")]
    [ApiController]
    public class UserAddressController : ControllerBase
    {
        private readonly IUserAddressService _userAddressService;
		public UserAddressController(IUserAddressService userAddressService)
		{
			_userAddressService = userAddressService;
		}
		[HttpPost]
		public IActionResult AddUserAddress([FromBody] CreateUpdateUserAddressRequest create)
		{
			var model = new CreateUpdateUserAddress
			{
				UserId = create.UserId,
				Address = create.Address,
				Iat = create.Iat,
				Ing = create.Ing,
				isDefault = create.isDefault
			};
			var result = _userAddressService.AddUserAddress(model);
			if (!result)
			{
				return BadRequest(new { message = "Failed to add user address. User may not exist." });
			}
			return Ok(new { message = "User address added successfully." });
		}
		[HttpDelete("{userAddressId}")]
		public IActionResult DeleteUserAddress([FromRoute] Guid userAddressId)
		{
			var result = _userAddressService.DeleteUserAddress(userAddressId);
			if (!result)
			{
				return NotFound(new { message = "User address not found." });
			}
			return Ok(new { message = "User address deleted successfully." });
		}
		[HttpGet("{userId}")]
		public IActionResult GetUserAddressByUserId([FromRoute] Guid userId)
		{
			var address = _userAddressService.GetByUserId(userId);
			if (address == null)
			{
				return NotFound(new { message = "User address not found." });
			}
			return Ok(address);
		}
		[HttpPut("{userAddressId}")]
		public IActionResult UpdateUserAddress([FromRoute] Guid userAddressId, [FromBody] CreateUpdateUserAddressRequest update)
		{
			var model = new CreateUpdateUserAddress
			{
				UserId = update.UserId,
				Address = update.Address,
				Iat = update.Iat,
				Ing = update.Ing,
				isDefault = update.isDefault
			};
			var result = _userAddressService.UpdateUserAddress(userAddressId, model);
			if (!result)
			{
				return NotFound(new { message = "User address not found." });
			}
			return Ok(new { message = "User address updated successfully." });
		}
	}
}

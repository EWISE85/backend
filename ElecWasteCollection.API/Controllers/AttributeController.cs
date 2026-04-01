using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/attributes")]
    [ApiController]
    public class AttributeController : ControllerBase
    {
        private readonly IAttributeService _attributeService;
		public AttributeController(IAttributeService attributeService)
		{
			_attributeService = attributeService;
		}
		[HttpGet("admin")]
		public async Task<IActionResult> GetAttributesForAdmin([FromQuery] string? status)
		{
			var pagedResult = await _attributeService.GetAttributeForAdmin(status);
			return Ok(pagedResult);
		}

	}
}

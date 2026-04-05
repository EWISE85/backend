using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ElecWasteCollection.API.Controllers
{
	[Authorize]
	[Route("api/attribute-option")]
    [ApiController]
    public class AttributeOptionController : ControllerBase
    {
        private readonly IAttributeOptionService _attributeOptionService;
		public AttributeOptionController(IAttributeOptionService attributeOptionService)
		{
			_attributeOptionService = attributeOptionService;
		}
		[HttpGet("by-attribute/{attributeId}")]
		public async Task<IActionResult> GetOptionsByAttributeId(Guid attributeId)
		{
			var options = await _attributeOptionService.GetOptionsByAttributeId(attributeId);
			return Ok(options);
		}
		[HttpGet("{optionId}")]
		public async Task<IActionResult> GetOptionByOptionId(Guid optionId)
		{
			var option = await _attributeOptionService.GetOptionByOptionId(optionId);
			if (option == null) return NotFound(new { Message = "Không tìm thấy option" });
			return Ok(option);
		}
		[HttpGet("admin/by-attribute/{attributeId}")]
		public async Task<IActionResult> GetOptionsByAttributeIdForAdmin(Guid attributeId, [FromQuery] string? status)
		{
			var options = await _attributeOptionService.GetOptionsByAttributeIdForAdmin(attributeId, status);
			return Ok(options);
		}

	}
}

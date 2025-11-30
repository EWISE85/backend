using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
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
		public IActionResult GetOptionsByAttributeId(Guid attributeId)
		{
			var options = _attributeOptionService.GetOptionsByAttributeId(attributeId);
			return Ok(options);
		}
	}
}

using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/system-config")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;
		public SystemConfigController(ISystemConfigService systemConfigService)
		{
			_systemConfigService = systemConfigService;
		}
		[HttpGet("active")]
		public IActionResult GetAllActiveSystemConfigs()
		{
			var configs = _systemConfigService.GetAllSystemConfigActive();
			return Ok(configs);
		}
		[HttpGet("{key}")]
		public IActionResult GetSystemConfigByKey(string key)
		{
			var config = _systemConfigService.GetSystemConfigByKey(key);
			if (config == null)
			{
				return NotFound("System configuration not found.");
			}
			return Ok(config);
		}
		[HttpPut("{id}")]
		public IActionResult UpdateSystemConfig([FromRoute] Guid id, [FromBody] UpdateSystemConfigRequest request)
		{ 

			var model = new UpdateSystemConfigModel
			{
				SystemConfigId = id,
				Value = request.Value
			};
			bool updateResult = _systemConfigService.UpdateSystemConfig(model);
			if (!updateResult)
			{
				return StatusCode(500, "An error occurred while updating the system configuration.");
			}

			return Ok(new { message = "System configuration updated successfully." });
		}
	}
}

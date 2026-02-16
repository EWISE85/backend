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
		public async Task<IActionResult> GetAllActiveSystemConfigs([FromQuery] SystemConfigFilterRequest request)
		{
			var configs = await _systemConfigService.GetAllSystemConfigActive(request.GroupName);
			return Ok(configs);
		}
		[HttpGet("{key}")]
		public async Task<IActionResult> GetSystemConfigByKey(string key)
		{
			var config = await _systemConfigService.GetSystemConfigByKey(key);
			if (config == null)
			{
				return NotFound("System configuration not found.");
			}
			return Ok(config);
		}
		[HttpPut("{id}")]
		[Consumes("multipart/form-data")]
		public async Task<IActionResult> UpdateSystemConfig([FromRoute] Guid id, [FromForm] UpdateSystemConfigRequest request)
		{ 

			var model = new UpdateSystemConfigModel
			{
				SystemConfigId = id,
				Value = request.Value,
				ExcelFile = request.ExcelFile
			};
			bool updateResult = await _systemConfigService.UpdateSystemConfig(model);
			if (!updateResult)
			{
				return StatusCode(500, "An error occurred while updating the system configuration.");
			}

			return Ok(new { message = "System configuration updated successfully." });
		}

		[HttpPost("upload-excel")]
		public async Task<IActionResult> UploadExcelAndSaveConfig(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			bool result = await _systemConfigService.CreateNewConfigWithFileAsync(file);
			if (!result)
			{
				return StatusCode(500, "An error occurred while uploading the file and saving the configuration.");
			}

			return Ok(new { message = "File uploaded and configuration saved successfully." });
		}
		[HttpGet("download/{id}")]
		public async Task<IActionResult> DownloadExcel(Guid id)
		{
			try
			{
				var (fileBytes, fileName) = await _systemConfigService.DownloadFileByConfigIdAsync(id);

				string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

				return File(fileBytes, contentType, fileName);
			}
			catch (Exception ex)
			{
				return NotFound(new { message = ex.Message });
			}
		}
		[HttpGet("server-time")]
		public IActionResult GetServerTime()
		{
			return Ok(new
			{
				serverTime = DateTime.Now,
				serverDate = DateOnly.FromDateTime(DateTime.Now)
			});
		}

        [HttpGet("speed")]
        public async Task<IActionResult> GetSpeeds([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null)
        {
            var result = await _systemConfigService.GetWarehouseSpeedsPagedAsync(page, limit, search);
            return Ok(result);
        }

        [HttpPost("speed")]
        public async Task<IActionResult> SetSpeed([FromBody] WarehouseSpeedRequest request)
        {
            if (request.SpeedKmh <= 0)
            {
                return BadRequest(new { Message = "Tốc độ phải lớn hơn 0" });
            }

            var result = await _systemConfigService.UpsertWarehouseSpeedAsync(request);
            return Ok(new { Success = result, Message = "Cập nhật thành công" });
        }

        [HttpPut("speed")]
        public async Task<IActionResult> UpdateSpeed([FromBody] WarehouseSpeedRequest request)
        {
            if (request.SpeedKmh <= 0)
            {
                return BadRequest(new { Message = "Tốc độ phải lớn hơn 0" });
            }

            var result = await _systemConfigService.UpdateWarehouseSpeedAsync(request);

            if (result)
            {
                return Ok(new { Success = true, Message = "Cập nhật tốc độ thành công" });
            }

            return BadRequest(new { Success = false, Message = "Cập nhật thất bại" });
        }

        [HttpDelete("speed/{smallPointId}")]
        public async Task<IActionResult> DeleteSpeed(string smallPointId)
        {
            var result = await _systemConfigService.DeleteWarehouseSpeedAsync(smallPointId);

            if (!result) return NotFound(new { Message = "Không tìm thấy cấu hình" });

            return Ok(new { Message = "Đã xóa cấu hình tốc độ thành công" });
        }
    }
}

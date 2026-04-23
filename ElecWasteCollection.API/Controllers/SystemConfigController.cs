using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Authorize]
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
            var result = await _systemConfigService.GetAllSystemConfigActive(
                request.GroupName,
                request.CompanyId,
                request.ScpId
            );

            if (result == null || !result.Any())
            {
                return Ok(new List<object>());
            }

            return Ok(result);
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
			TimeSpan vnOffset = TimeSpan.FromHours(7);

			DateTimeOffset serverTime = DateTimeOffset.UtcNow.ToOffset(vnOffset);

			// 3. Trả về kết quả
			return Ok(new
			{
				serverTime = serverTime.ToString("yyyy-MM-ddTHH:mm:sszzz"),
				serverDate = DateOnly.FromDateTime(serverTime.DateTime)
			});

		}

		[HttpGet("speed")]
        public async Task<IActionResult> GetSpeeds([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string? search = null)
        {
            var result = await _systemConfigService.GetWarehouseSpeedsPagedAsync(page, limit, search);
            return Ok(result);
        }

        [HttpGet("speed/{smallPointId}")]
        public async Task<IActionResult> GetSpeedByPointId(string smallPointId)
        {
            try
            {
                var result = await _systemConfigService.GetWarehouseSpeedByPointIdAsync(smallPointId);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
            }
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


        [HttpGet("auto-assign-settings")]
        public async Task<IActionResult> GetAutoAssignSettings()
        {
            var settings = await _systemConfigService.GetAutoAssignSettingsAsync();
            return Ok(new { success = true, data = settings });
        }

        [HttpPut("auto-assign-settings")]
        public async Task<IActionResult> UpdateAutoAssignSettings([FromBody] UpdateAutoAssignRequest model)
        {
            var result = await _systemConfigService.UpdateAutoAssignSettingsAsync(model);
            if (result)
                return Ok(new { success = true, message = "Cập nhật cấu hình tự động thành công." });

            return BadRequest(new { success = false, message = "Không có thay đổi nào được thực hiện." });
        }

        [HttpGet("load-threshold")]
        public async Task<IActionResult> GetLoadThreshold()
        {
            var result = await _systemConfigService.GetWarehouseLoadThresholdAsync();
            return Ok(new { success = true, data = result });
        }

        [HttpPut("warehouse-load-threshold")]
        public async Task<IActionResult> UpdateWarehouseLoadThreshold([FromBody] UpdateThresholdRequest model)
        {
            // Kiểm tra dữ liệu đầu vào cơ bản
            if (string.IsNullOrEmpty(model.SmallCollectionPointId))
            {
                return BadRequest(new { success = false, message = "ID điểm thu gom không được để trống." });
            }

            // Gọi hàm Service bạn đã viết
            var result = await _systemConfigService.UpdateWarehouseLoadThresholdAsync(model);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = $"Cập nhật ngưỡng tải trọng cho kho {model.SmallCollectionPointId} thành công."
                });
            }

            // Trường hợp không có thay đổi (ví dụ Admin nhấn Save nhưng giá trị cũ vẫn thế)
            return Ok(new { success = true, message = "Không có thay đổi nào được thực hiện." });
        }

    }
}

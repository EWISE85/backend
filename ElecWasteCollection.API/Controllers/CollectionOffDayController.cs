using Microsoft.AspNetCore.Mvc;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.IServices.ElecWasteCollection.Application.IServices;

namespace ElecWasteCollection.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CollectionOffDayController : ControllerBase
    {
        private readonly ICollectionOffDayService _offDayService;

        public CollectionOffDayController(ICollectionOffDayService offDayService)
        {
            _offDayService = offDayService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> RegisterOffDays([FromBody] RegisterOffDayRequest request)
        {
            if (request.OffDates == null || !request.OffDates.Any())
                return BadRequest("Vui lòng chọn ít nhất một ngày nghỉ.");

            var success = await _offDayService.RegisterOffDaysAsync(request);
            return success ? Ok(new { Message = "Đăng ký lịch nghỉ thành công." }) : StatusCode(500, "Không thể lưu lịch nghỉ.");
        }

        [HttpGet("available-for-assign")]
        public async Task<IActionResult> GetAvailable([FromQuery] DateOnly date)
        {
            var data = await _offDayService.GetAvailableCompaniesForAssignAsync(date);
            return Ok(data);
        }
        [HttpDelete("cancel")]
        public async Task<IActionResult> CancelOffDay([FromQuery] string? companyId, [FromQuery] string? pointId, [FromQuery] DateOnly date)
        {
            var success = await _offDayService.RemoveOffDayAsync(companyId, pointId, date);
            return success ? Ok("Đã hủy lịch nghỉ thành công.") : NotFound("Không tìm thấy lịch nghỉ để hủy.");
        }
        [HttpGet("all-off-days")]
        public async Task<IActionResult> GetAllOffDays(
            [FromQuery] string? companyId,
            [FromQuery] DateOnly? date,
            [FromQuery] string? smallCollectionPointId,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var result = await _offDayService.GetAllOffDaysAsync(companyId, date, smallCollectionPointId, page, limit);
            return Ok(result);
        }
    }
}
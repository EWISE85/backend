using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Authorize]
	[Route("api/report")]
	[ApiController]
	public class ReportController : ControllerBase
	{
		private readonly IReportService _reportService;

		public ReportController(IReportService reportService)
		{
			_reportService = reportService;
		}
		[HttpPost()]
		public async Task<IActionResult> CreateReport([FromBody] ReportRequest request)
		{
			var createReportModel = new CreateReportModel
			{
				UserId = request.UserId,
				ProductId = request.ProductId,
				Description = request.Description,
				ReportType = request.ReportType,
				Images = request.Images
			};
			var result = await _reportService.CreateReport(createReportModel);
			if (result)
			{
				return Ok(new { Message = "Báo cáo đã được tạo thành công." });
			}
			else
			{
				return BadRequest(new { Message = "Không thể tạo báo cáo." });
			}
		}
		[HttpGet("filter")]
		public async Task<IActionResult> GetPagedReport([FromQuery] UserReportQueryModel queryModel)
		{
			var pagedReports = await _reportService.GetPagedReport(queryModel);
			return Ok(pagedReports);
		}

		[HttpGet("user/filter")]
		public async Task<IActionResult> GetPagedReportForUser([FromQuery] UserReportQueryModel queryModel)
		{
			var pagedReports = await _reportService.GetPagedReportForUser(queryModel);
			return Ok(pagedReports);
		}
		[HttpPut("answer/{id}")]
		public async Task<IActionResult> AnswerReport(Guid id, [FromBody] string answerMessage)
		{
			var result = await _reportService.AnswerReport(id, answerMessage);
			if (result)
			{
				return Ok(new { Message = "Đã trả lời khiếu nại thành công." });
			}
			else
			{
				return BadRequest(new { Message = "Không thể trả lời khiếu nại." });
			}
		}
		[HttpGet("type")]
		public async Task<IActionResult> GetReportTypes()
		{
			var reportTypes = await _reportService.GetReportTypes();

			return Ok(reportTypes);
		}
		[HttpGet("{reportId}")]
		public async Task<IActionResult> GetReport(Guid reportId)
		{
			var report = await _reportService.GetReport(reportId);
			if (report == null)
			{
				return NotFound(new { Message = "Không tìm thấy khiếu nại." });
			}
			return Ok(report);
		}

		}

}

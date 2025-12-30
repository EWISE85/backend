using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/dashboard")]
	[ApiController]
	public class DashboardController : ControllerBase
	{
		private readonly IDashboardService _dashboardService;
		public DashboardController(IDashboardService dashboardService)
		{
			_dashboardService = dashboardService;
		}
		[HttpGet("summary")]
		public async Task<IActionResult> GetDashboardSummary([FromQuery] DateOnly from, [FromQuery] DateOnly to)
		{
			var summary = await _dashboardService.GetDashboardSummary(from, to);
			return Ok(summary);
		}

	}
}

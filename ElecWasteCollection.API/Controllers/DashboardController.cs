using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
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
        [HttpGet("package-stats")]
        public async Task<IActionResult> GetPackageStats( [FromQuery] string smallId, [FromQuery] DateOnly fromDate, [FromQuery] DateOnly toDate)
        {
            try
            {
                var result = await _dashboardService.GetPackageDashboardStats(smallId, fromDate, toDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpGet("dashboard/collected-products")]
        public async Task<IActionResult> GetCollectedProductDashboard( [FromQuery] string smallCollectionPointId, [FromQuery] DateOnly from,[FromQuery] DateOnly to)
        {
            var result = await _dashboardService.GetCollectedProductStatsAsync(
                smallCollectionPointId, from, to);

            return Ok(result);
        }

    }
}

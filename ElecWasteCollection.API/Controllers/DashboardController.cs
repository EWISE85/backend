using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
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

        [HttpGet("summary/day")]
        public async Task<IActionResult> GetDashboardSummaryByDay( [FromQuery] DateOnly date)
        {
            var result = await _dashboardService.GetDashboardSummaryByDay(date);
            return Ok(result);
        }

        [HttpGet("packages-stats")]
        public async Task<IActionResult> GetDashboardStats( [FromQuery] string smallCollectionPointId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            if (string.IsNullOrWhiteSpace(smallCollectionPointId))
            {
                return BadRequest("SmallCollectionPointId is required.");
            }

            if (from > to)
            {
                return BadRequest("'From Date' cannot be greater than 'To Date'.");
            }

            try
            {
                var result = await _dashboardService.GetPackageDashboardStats(smallCollectionPointId, from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("scp/summary")]
        public async Task<IActionResult> GetSCPDashboardSummary(
            [FromQuery] string smallCollectionPointId,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            if (string.IsNullOrWhiteSpace(smallCollectionPointId))
            {
                return BadRequest("SmallCollectionPointId is required.");
            }

            if (from > to)
            {
                return BadRequest("'From Date' cannot be greater than 'To Date'.");
            }

            try
            {
                var result = await _dashboardService.GetSCPDashboardSummary(smallCollectionPointId, from, to);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("scp/summary-by-day")]
        public async Task<IActionResult> GetSCPDashboardSummaryByDay(
            [FromQuery] string smallCollectionPointId,
            [FromQuery] DateOnly date)
        {
            if (string.IsNullOrWhiteSpace(smallCollectionPointId))
            {
                return BadRequest("SmallCollectionPointId is required.");
            }

            try
            {
                var result = await _dashboardService.GetSCPDashboardSummaryByDay(smallCollectionPointId, date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}

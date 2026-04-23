using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ElecWasteCollection.API.Controllers
{
	[Authorize]
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
        [HttpGet("system/brands/summary")]
        public async Task<IActionResult> GetGlobalStats([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var result = await _dashboardService.GetGlobalBrandDashboardStats(from, to);
            return Ok(result);
        }

        [HttpGet("system/brands/by-day")]
        public async Task<IActionResult> GetGlobalByDay([FromQuery] DateOnly date)
        {
            var result = await _dashboardService.GetGlobalBrandDashboardStatsByDay(date);
            return Ok(result);
        }
        [HttpGet("system/topUser/all")]
        public async Task<IActionResult> GetGlobalTopContributors(
            [FromQuery] int top,
            [FromQuery] DateOnly from,
            [FromQuery] DateOnly to)
        {
            var result = await _dashboardService.GetGlobalTopUsers(top, from, to);
            return Ok(result);
        }

        [HttpGet("scp/{scpId}/brands/summary")]
        public async Task<IActionResult> GetBrandStats(string scpId, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var result = await _dashboardService.GetBrandDashboardStats(scpId, from, to);
            return Ok(result);
        }
        [HttpGet("scp/{scpId}/brands/by-day")]
        public async Task<IActionResult> GetBrandStatsByDay(string scpId, [FromQuery] DateOnly date)
        {
            var result = await _dashboardService.GetBrandDashboardStatsByDay(scpId, date);
            return Ok(result);
        }
        [HttpGet("scp/{scpId}/topUser")]
        public async Task<IActionResult> GetTopContributors(string scpId, [FromQuery] int top, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        {
            var result = await _dashboardService.GetTopUsers(scpId, top, from, to);
            return Ok(result);
        }

        [HttpGet("user/{userId}/topUserdetails")]
        public async Task<IActionResult> GetDetails(Guid userId)
        {
            var result = await _dashboardService.GetUserProductDetails(userId);
            return Ok(result);
        }
        [HttpGet("admin/brand-details")]
        public async Task<IActionResult> GetAdminBrandDetails(
    [FromQuery] string? scpId,
    [FromQuery] string brandName,
    [FromQuery] DateOnly from,
    [FromQuery] DateOnly to,
    [FromQuery] int page = 1,
    [FromQuery] int limit = 10)
        {
            var result = await _dashboardService.GetBrandDetailsAsync(scpId, brandName, from, to, page, limit);
            return Ok(result);
        }
    }
}

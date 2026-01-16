using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model.GroupModel;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [ApiController]
    [Route("api/grouping")]
    public class GroupingController : ControllerBase
    {
        private readonly IGroupingService _groupingService;

        public GroupingController(IGroupingService groupingService)
        {
            _groupingService = groupingService;
        }

        [HttpPost("pre-assign")]
        public async Task<IActionResult> PreAssign([FromBody] PreAssignRequest request)
        {
            var result = await _groupingService.PreAssignAsync(request);
            return Ok(result);
        }

        [HttpGet("preview-products")]
        public async Task<IActionResult> GetPreviewProducts([FromQuery] string vehicleId, [FromQuery] DateOnly workDate)
        {
            var result = await _groupingService.GetPreviewProductsAsync(vehicleId, workDate);
            return Ok(result);
        }

        [HttpGet("preview-vehicles")]
        public async Task<IActionResult> GetPreviewVehicles([FromQuery] DateOnly workDate)
        {
            try
            {
                var result = await _groupingService.GetPreviewVehiclesAsync(workDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("assign-day")]
        public async Task<IActionResult> AssignDay([FromBody] AssignDayRequest request)
        {
            bool ok = await _groupingService.AssignDayAsync(request);
            return Ok(new { success = ok });
        }


        [HttpPost("auto-group")]
        public async Task<IActionResult> GroupByCollectionPointAsync([FromBody] GroupingByPointRequest request)
        {
            var result = await _groupingService.GroupByCollectionPointAsync(request);
            return Ok(result);
        }

        [HttpGet("groups/{collectionPointId}")]
        public async Task<IActionResult> GetGroupsByCollectionPointAsync(string collectionPointId)
        {
            var result = await _groupingService.GetGroupsByPointIdAsync(collectionPointId);
            return Ok(result);
        }

        [HttpGet("group/{groupId}")]
        public async Task<IActionResult> GetGroupDetailAsync(int groupId)
        {
            var result = await _groupingService.GetRoutesByGroupAsync(groupId);
            return Ok(result);
        }

        [HttpGet("vehicles")]
        public async Task<IActionResult> GetVehicles()
        {
            var result = await _groupingService.GetVehiclesAsync();
            return Ok(result);
        }

        [HttpGet("posts/pending-grouping")]
        public async Task<IActionResult> GetPendingPosts()
        {
            var result = await _groupingService.GetPendingPostsAsync();
            return Ok(result);
        }

        [HttpGet("vehicles/{SmallCollectionPointId}")]
        public async Task<IActionResult> GetVehiclesBySmallPointAsync(string SmallCollectionPointId)
        {
            var result = await _groupingService.GetVehiclesBySmallPointAsync(SmallCollectionPointId);
            return Ok(result);
        }

        [HttpPost("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdatePointSettingRequest request)
        {
            try
            {
                var result = await _groupingService.UpdatePointSettingAsync(request);
                return Ok(new { message = "Cập nhật thành công", success = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpGet("company/settings/{companyId}")]
        public async Task<IActionResult> GetCompanySettings(string companyId)
        {
            try
            {
                var result = await _groupingService.GetCompanySettingsAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("settings/{pointId}")]
        public async Task<IActionResult> GetSettings(string pointId)
        {
            try
            {
                var result = await _groupingService.GetPointSettingAsync(pointId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

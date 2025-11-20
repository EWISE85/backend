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

        [HttpGet("routes/{groupId}")]
        public async Task<IActionResult> GetRoutes(int groupId)
        {
            var result = await _groupingService.GetRoutesByGroupAsync(groupId);
            if (!result.Any()) return NotFound("Không có route.");
            return Ok(result);
        }


        [HttpGet("assign/{pointId}")]
        public async Task<IActionResult> GetAssign(int pointId)
        {
            var result = await _groupingService.GetAssignByPointAsync(pointId);
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


    }
}

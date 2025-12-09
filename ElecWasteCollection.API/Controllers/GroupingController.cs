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

        [HttpGet("groups/{collectionPointId}")]
        public async Task<IActionResult> GetGroupsByCollectionPointAsync(int collectionPointId)
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
        public async Task<IActionResult> GetVehiclesBySmallPointAsync(int SmallCollectionPointId)
        {
            var result = await _groupingService.GetVehiclesBySmallPointAsync(SmallCollectionPointId);
            return Ok(result);
        }

        [HttpPost("reassign-collector")]
        public async Task<IActionResult> ReassignCollector([FromBody] ReassignGroupRequest request)
        {
            try
            {
                // Service bây giờ trả về object ReassignGroupResponse
                var result = await _groupingService.ReassignGroupAsync(request);

                // Trả về JSON phẳng cho Client dễ dùng
                return Ok(new
                {
                    Success = true,
                    Message = result.Message,
                    GroupId = result.GroupId,
                    CollectorName = result.CollectorName
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message,
                    GroupId = 0,
                    CollectorName = (string?)null
                });
            }
        }


    }
}

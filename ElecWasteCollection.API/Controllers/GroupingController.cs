//using ElecWasteCollection.Application.Interfaces;
//using ElecWasteCollection.Application.Model;
//using Microsoft.AspNetCore.Mvc;

//namespace ElecWasteCollection.API.Controllers
//{
//    [ApiController]
//    [Route("api/grouping")]
//    public class GroupingController : ControllerBase
//    {
//        private readonly IGroupingService _groupingService;

//        public GroupingController(IGroupingService groupingService)
//        {
//            _groupingService = groupingService;
//        }

//        [HttpPost("auto-group")]
//        public async Task<IActionResult> AutoGroup([FromBody] GroupingByPointRequest request)
//        {
//            var result = await _groupingService.GroupByCollectionPointAsync(request);
//            return Ok(result);
//        }

//        [HttpPost("pre-assign")]
//        public async Task<IActionResult> PreAssign([FromBody] PreAssignRequest req)
//        {
//            var result = await _groupingService.PreAssignAsync(req);
//            return Ok(result);
//        }

//        [HttpPost("assign-day")]
//        public async Task<IActionResult> AssignDay([FromBody] AssignDayRequest req)
//        {
//            var result = await _groupingService.AssignDayAsync(req);
//            return Ok(new { success = result });
//        }
//    }
//}



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
    }
}

using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model;
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

        [HttpPost("auto-group")]
        public async Task<IActionResult> AutoGroup([FromBody] GroupingByPointRequest request)
        {
            var result = await _groupingService.GroupByCollectionPointAsync(request);
            return Ok(result);
        }
    }
}

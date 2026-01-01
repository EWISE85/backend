using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecyclingQueryController : ControllerBase
    {
        private readonly IRecyclingQueryService _queryService;

        public RecyclingQueryController(IRecyclingQueryService queryService)
        {
            _queryService = queryService;
        }

        [HttpGet("tasks")]
        public async Task<IActionResult> GetTasks([FromQuery] string recyclingCompanyId)
        {
            if (string.IsNullOrEmpty(recyclingCompanyId))
                return BadRequest(new { message = "Vui lòng truyền recyclingCompanyId." });

            try
            {
                var result = await _queryService.GetPackagesToCollectAsync(recyclingCompanyId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReassignDriverController : ControllerBase
    {
        private readonly IReassignDriverService _service;

        public ReassignDriverController(IReassignDriverService service)
        {
            _service = service;
        }
        [HttpGet("candidates")]
        public async Task<IActionResult> GetCandidates([FromQuery] string companyId, [FromQuery] DateTime date)
        {
            try
            {
                if (string.IsNullOrEmpty(companyId))
                {
                    return BadRequest(new { Success = false, Message = "CompanyId is required." });
                }
                var result = await _service.GetReassignCandidatesAsync(companyId, date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
        [HttpPost("confirm")]
        public async Task<IActionResult> ConfirmReassign([FromBody] ReassignDriverRequest request)
        {
            try
            {
                var result = await _service.ReassignDriverAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Success = false, Message = ex.Message });
            }
        }
    }
}

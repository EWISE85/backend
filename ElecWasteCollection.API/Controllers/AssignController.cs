using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [ApiController]
    [Route("api/assign")]
    public class AssignController : ControllerBase
    {
        private readonly ITeamAssignService _teamService;
        private readonly ISmallPointAssignService _smallPointService;
        private readonly ITeamRatioService _ratioService;

        public AssignController(
            ITeamAssignService teamService,
            ISmallPointAssignService smallPointService,
            ITeamRatioService ratioService)
        {
            _teamService = teamService;
            _smallPointService = smallPointService;
            _ratioService = ratioService;
        }

        [HttpPost("team-ratio")]
        public async Task<IActionResult> UpdateRatio([FromBody] TeamRatioConfigRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var data = await _ratioService.UpdateRatios(req);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("team")]
        public async Task<IActionResult> AssignTeam([FromBody] AssignTeamRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            try
            {
                var data = await _teamService.AssignPostsToTeamsAsync(req);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("smallpoint/{teamId}")]
        public async Task<IActionResult> AssignSmallPoint(int teamId)
        {
            try
            {
                var data = await _smallPointService.AssignSmallPointsAsync(teamId);
                return Ok(data);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}

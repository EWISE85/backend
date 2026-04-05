using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RankController : ControllerBase
    {
        private readonly IRankService _rankService;

        public RankController(IRankService rankService)
        {
            _rankService = rankService;
        }

        [HttpGet("progress/{userId}")]
        public async Task<IActionResult> GetUserProgress(Guid userId)
        {
            var result = await _rankService.GetRankProgressAsync(userId);
            return result != null ? Ok(result) : NotFound("Không tìm thấy người dùng.");
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllRanks()
        {
            var ranks = await _rankService.GetAllRanksAsync();
            return Ok(ranks);
        }

        [HttpGet("leaderboard")]
        public async Task<IActionResult> GetLeaderboard([FromQuery] int top = 10)
        {
            var leaderboard = await _rankService.GetTopGreenUsersAsync(top);
            return Ok(leaderboard);
        }
    }
}
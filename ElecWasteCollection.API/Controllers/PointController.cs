using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/")]
    [ApiController]
    public class PointController : ControllerBase
    {
        private readonly IUserPointService _userPointService;
        private readonly IPointTransactionService _pointTransactionService;
        public PointController(IUserPointService userPointService, IPointTransactionService pointTransactionService)
        {
            _userPointService = userPointService;
            _pointTransactionService = pointTransactionService;
        }
        [HttpGet("points/{userId}")]
        public async Task<IActionResult> GetUserPoints([FromRoute] Guid userId)
        {
            var points = await _userPointService.GetPointByUserId(userId);
            return Ok(points);
        }
        [HttpPost("points-transaction")]
        public async Task<IActionResult> CreatePointTransaction([FromBody] ReceivePointFromCollectionPointRequest request)
        {
            var model = new CreatePointTransactionModel
            {
                UserId = request.UserId,
                Point = request.Point,
                Desciption = request.Desciption,
            };
            var result = await _pointTransactionService.ReceivePointFromCollectionPoint(model);
            return Ok(result);
        }
        [HttpGet("points-transaction/{userId}")]
        public async Task<IActionResult> GetPointTransactionByUserId([FromRoute] Guid userId)
        {
            var pointTransactions = await _pointTransactionService.GetAllPointHistoryByUserId(userId);
            return Ok(pointTransactions);
        }

    }
}
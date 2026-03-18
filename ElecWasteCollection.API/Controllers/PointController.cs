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
        private readonly IUserService _userService;
        private readonly IPointTransactionService _pointTransactionService;
        public PointController(IPointTransactionService pointTransactionService, IUserService userService)
        {
            _pointTransactionService = pointTransactionService;
			_userService = userService;
		}
        [HttpGet("points/{userId}")]
        public async Task<IActionResult> GetUserPoints([FromRoute] Guid userId)
        {
            var points = await _userService.GetPointByUserId(userId);
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
            var result = await _pointTransactionService.ReceivePointFromCollectionPoint(model,true);
            return Ok(result);
        }
        [HttpGet("points-transaction/{userId}")]
        public async Task<IActionResult> GetPointTransactionByUserId([FromRoute] Guid userId)
        {
            var pointTransactions = await _pointTransactionService.GetAllPointHistoryByUserId(userId);
            return Ok(pointTransactions);
        }
        [HttpPut("points-transaction/{productId}")]
		public async Task<IActionResult> UpdatePointByProductId([FromRoute] Guid productId, [FromBody] UpdatePointTransactionRequest request)
		{
			var result = await _pointTransactionService.UpdatePointByProductId(productId, request.NewPointValue, request.ReasonForUpdate);
			return Ok(result);
		}

        [HttpPost("point/daily")]
		public async Task<IActionResult> ReceiveDailyPoint([FromBody] ReceiveDailyPointRequest request)
		{
			var result = await _pointTransactionService.ReceivePointDaily(request.UserId, request.Points);
			if (result)
			{
				return Ok(new { Message = "Nhận điểm hàng ngày thành công" });
			}
			else
			{
				return BadRequest(new { Message = "Nhận điểm hàng ngày thất bại" });
			}
		}
}
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/tracking")]
	[ApiController]
	public class TrackingController : ControllerBase
	{
		private readonly ITrackingService _trackingService;
		public TrackingController(ITrackingService trackingService)
		{
			_trackingService = trackingService;
		}
		//[HttpGet("collection-timeline/{collectionRouteId}")]
		//public async Task<IActionResult> GetCollectionTimeline(Guid collectionRouteId)
		//{
		//	var timeline = await _trackingService.GetCollectionTimelineAsync(collectionRouteId);
		//	return Ok(timeline);
		//}
		//[HttpGet("product-history/{productId}")]
		//public async Task<IActionResult> GetProductHistory(Guid productId)
		//{
		//	var history = await _trackingService.GetProductHistoryAsync(productId);
		//	return Ok(history);
		//}
		[HttpGet("post/{postId}/timeline")]
		public async Task<IActionResult> GetFullPostTimeline([FromRoute]Guid postId)
		{
			var timeline = await _trackingService.GetFullTimelineByPostIdAsync(postId);

			if (timeline == null || !timeline.Any())
			{
				return NotFound("Không tìm thấy lịch sử cho yêu cầu này.");
			}
			return Ok(timeline);
		}
	}
}

using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/routes/")]
	[ApiController]
	public class CollectionRouteController : ControllerBase
	{
		private readonly ICollectionRouteService _collectionRouteService;
		public CollectionRouteController(ICollectionRouteService collectionRouteService)
		{
			_collectionRouteService = collectionRouteService;
		}
		[HttpGet("{pickUpDate}")]
		public IActionResult GetAllRoutes(DateOnly pickUpDate)
		{
			var routes = _collectionRouteService.GetAllRoutes(pickUpDate);
			return Ok(routes);
		}
		[HttpGet("{pickUpDate}/collector/{id}")]
		public IActionResult GetRoutesByCollectorId([FromRoute] DateOnly pickUpDate, [FromRoute] Guid id)
		{
			var routes = _collectionRouteService.GetRoutesByCollectorId(pickUpDate, id);
			return Ok(routes);
		}
		[HttpGet("collection-point/date/filter")]
		public IActionResult GetRoutesByCollectionPointId([FromQuery] RouteSearchQueryRequest searchQueryRequest)
		{
			var model = new RouteSearchQueryModel
			{
				Page = searchQueryRequest.Page,
				Limit = searchQueryRequest.Limit,
				CollectionPointId = searchQueryRequest.CollectionPointId,
				PickUpDate = searchQueryRequest.PickUpDate,
				Status = searchQueryRequest.Status
			};
			var routes = _collectionRouteService.GetPagedRoutes(model);
			return Ok(routes);
		}

		[HttpGet("detail/{collectionRouteId}")]
		public IActionResult GetRouteById(Guid collectionRouteId)
		{
			var route = _collectionRouteService.GetRouteById(collectionRouteId);
			if (route == null)
			{
				return NotFound($"Collection route with ID {collectionRouteId} not found.");
			}
			return Ok(route);
		}
		[HttpPut("confirm/{collectionRouteId}")]
		public IActionResult ConfirmCollection(Guid collectionRouteId, [FromBody] ConfirmCollectionRequest request)
		{
			var result = _collectionRouteService.ConfirmCollection(collectionRouteId, request.ConfirmImages, request.QRCode);
			if (!result)
			{
				return StatusCode(400, "An error occurred while confirming the collection.");
			}
			return Ok(new { message = "Collection confirmed successfully." });
		}
		[HttpPut("cancel/{collectionRouteId}")]
		public IActionResult CancelCollection(Guid collectionRouteId, [FromBody] CancelCollectionRequest rejectMessage)
		{
			var result = _collectionRouteService.CancelCollection(collectionRouteId, rejectMessage.RejectMessage);
			if (!result)
			{
				return StatusCode(400, "An error occurred while canceling the collection.");
			}
			return Ok(new { message = "Collection canceled successfully." });
		}

		[HttpPut("user-confirm/{collectionRouteId}")]
		public async Task<IActionResult> IsUserConfirm(Guid collectionRouteId, [FromBody] UserConfirmRequest request)
		{
			var result = await _collectionRouteService.IsUserConfirm(collectionRouteId, request.IsConfirm, request.IsSkip);
			if (!result)
			{
				return StatusCode(400, "An error occurred while processing user confirmation.");
			}
			return Ok(new { message = "User confirmation processed successfully." });
		}
	}
}

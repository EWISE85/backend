using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/routes")]
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
		public IActionResult ConfirmCollection(Guid collectionRouteId, [FromBody] ConfirmCollectionRequest confirmImages)
		{
			var result = _collectionRouteService.ConfirmCollection(collectionRouteId, confirmImages.ConfirmImages);
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
	}
}

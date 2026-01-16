using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/image")]
	[ApiController]
	public class ImageController : ControllerBase
	{
		private readonly IImageComparisonService _imageComparisonService;
		public ImageController(IImageComparisonService imageComparisonService)
		{
			_imageComparisonService = imageComparisonService;
		}
		[HttpPost("compare-confirm")]
		public async Task<IActionResult> CompareImagesForConfirmation([FromBody] ImageComparisonRequest request)
		{
			if (request.ProductImages == null || request.ConfirmImages == null || !request.ProductImages.Any() || !request.ConfirmImages.Any())
			{
				return BadRequest("Both image lists must contain at least one image.");
			}

			bool areSimilar = await _imageComparisonService.CompareImagesSimilarityAsync(request.ProductImages, request.ConfirmImages);
			return Ok(new { AreSimilar = areSimilar });
		}
	}
}

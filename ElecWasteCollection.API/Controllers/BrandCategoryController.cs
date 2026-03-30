using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/brand-category")]
	[ApiController]
	public class BrandCategoryController : ControllerBase
	{
		private readonly IBrandCategoryService _brandCategoryService;

		public BrandCategoryController(IBrandCategoryService brandCategoryService)
		{
			_brandCategoryService = brandCategoryService;
		}
		[HttpGet("points/{categoryId}/{brandId}")]
		public async Task<IActionResult> GetPoints([FromRoute] Guid categoryId, [FromRoute] Guid brandId) 
		{
			var points = await _brandCategoryService.EstimatePointForBrandAndCategory(categoryId, brandId);
			return Ok(new
			{
				point = points
			});
		}

	}
}

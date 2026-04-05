using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Authorize]
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
		[HttpGet("admin/{categoryId}")]
		public async Task<IActionResult> GetPagedBrandForAdmin([FromRoute] Guid categoryId, [FromQuery] string? brandName, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 10)
		{
			var pagedResult = await _brandCategoryService.GetPagedBrandForAdmin(categoryId, brandName, status, page, limit);
			return Ok(pagedResult);
		}
		[HttpDelete("{categoryId}/{brandId}")]
		public async Task<IActionResult> DeleteBrandCategory([FromRoute] Guid categoryId, [FromRoute] Guid brandId)
		{
			var result = await _brandCategoryService.DeleteBrandCategory(categoryId, brandId);
			return Ok(new
			{
				success = result
			});
		}


	}
}

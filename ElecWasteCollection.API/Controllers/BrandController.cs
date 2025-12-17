using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/brands/")]
	[ApiController]
	public class BrandController : ControllerBase
	{
		private readonly IBrandService _brandService;
		public BrandController(IBrandService brandService)
		{
			_brandService = brandService;
		}
		[HttpGet("sub-category/{categoryId}")]
		public async Task<IActionResult> GetBrandsByCategoryId(Guid categoryId)
		{
			var brands = await _brandService.GetBrandsByCategoryIdAsync(categoryId);
			return Ok(brands);
		}
	}
}

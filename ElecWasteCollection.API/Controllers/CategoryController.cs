using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/categories")]
	[ApiController]
	public class CategoryController : ControllerBase
	{
		private readonly ICategoryService _categorySerivce;
		private readonly ICategoryAttributeService _categoryAttributeService;
		public CategoryController(ICategoryService categorySerivce, ICategoryAttributeService categoryAttributeService)
		{
			_categorySerivce = categorySerivce;
			_categoryAttributeService = categoryAttributeService;
		}
		[HttpGet("parents")]
		public async Task<IActionResult> GetParentCategories()
		{
			var parentCategories = await _categorySerivce.GetParentCategory();
			return Ok(parentCategories);
		}
		[HttpGet("{parentId}/subcategories")]
		public async Task<IActionResult> GetSubCategoriesByParentId(Guid parentId)
		{
			var subCategories = await _categorySerivce.GetSubCategoryByParentId(parentId);
			return Ok(subCategories);
		}
		[HttpGet("{subCategoryId}/attributes")]
		public async Task<IActionResult> GetAttributesByCategoryId([FromRoute]Guid subCategoryId)
		{
			var attributes = await _categoryAttributeService.GetCategoryAttributesByCategoryIdAsync(subCategoryId);
			return Ok(attributes);
		}
		[HttpGet("/subCategory")]
		public async Task<IActionResult> GetSubCategoriesByName([FromQuery]Guid parentId,[FromQuery] string name)
		{
			var subCategories = await _categorySerivce.GetSubCategoryByName(name, parentId);
			return Ok(subCategories);
		}

	}
}

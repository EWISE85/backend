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
		private readonly IExcelImportService _importService;
		public CategoryController(ICategoryService categorySerivce, ICategoryAttributeService categoryAttributeService, IExcelImportService importService)
		{
			_categorySerivce = categorySerivce;
			_categoryAttributeService = categoryAttributeService;
			_importService = importService;
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
		[HttpPost("system/import-excel")]
		public async Task<IActionResult> ImportCategorySystem(IFormFile file)
		{
			// 1. Kiểm tra file đầu vào
			if (file == null || file.Length == 0)
			{
				return BadRequest(new { Message = "Vui lòng chọn file Excel để upload." });
			}

			// 2. Kiểm tra định dạng file (chỉ nhận .xlsx)
			var extension = Path.GetExtension(file.FileName).ToLower();
			if (extension != ".xlsx")
			{
				return BadRequest(new { Message = "Hệ thống chỉ hỗ trợ định dạng file .xlsx" });
			}

			try
			{
				// 3. Mở stream và gọi Service xử lý
				using var stream = file.OpenReadStream();

				// "CategorySystem" là mã để Service biết cần đọc 5 sheet thần thánh của bạn
				var result = await _importService.ImportAsync(stream, "CategorySystem");

				if (result.Success)
				{
					return Ok(result);
				}

				return BadRequest(result);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = $"Lỗi hệ thống: {ex.Message}" });
			}
		}

		[HttpGet("admin/parents")]
		public async Task<IActionResult> GetParentCategoriesForAdmin([FromQuery] string? status)
		{
			var parentCategories = await _categorySerivce.GetParentCategoryForAdmin(status);
			return Ok(parentCategories);
		}
		[HttpGet("admin/child")]
		public async Task<IActionResult> GetSubCategoriesForAdmin([FromQuery] Guid parentId, [FromQuery] string? name, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int limit = 10)
		{
			var subCategories = await _categorySerivce.GetSubCategoryByParentIdForAdmin(parentId, name, status, page, limit);
			return Ok(subCategories);
		}
		//[HttpDelete("admin/child/{categoryId}")]
		//public async Task<IActionResult> DeleteChildCategory([FromRoute] Guid categoryId)
		//{
		//	var result = await _categorySerivce.DeleteChildCategory(categoryId);
		//	if (result)
		//	{
		//		return Ok(new { Message = "Xóa danh mục con thành công." });
		//	}
		//	return BadRequest(new { Message = "Không thể xóa danh mục con. Vui lòng thử lại." });
		//}
		//[HttpDelete("admin/parent/{categoryId}")]
		//public async Task<IActionResult> DeleteParentCategory([FromRoute] Guid categoryId)
		//{
		//	var result = await _categorySerivce.DeleteParentCategory(categoryId);
		//	if (result)
		//	{
		//		return Ok(new { Message = "Xóa danh mục cha thành công." });
		//	}
		//	return BadRequest(new { Message = "Không thể xóa danh mục cha. Vui lòng thử lại." });
		//}

		
	}
}

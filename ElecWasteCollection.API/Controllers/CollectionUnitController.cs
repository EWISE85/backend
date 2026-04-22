using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace ElecWasteCollection.API.Controllers
{

	[Authorize]
	[Route("api/collectionUnit")]
    [ApiController]
    public class CollectionUnitController : ControllerBase
    {
        private readonly ICollectionUnitService _smallCollectionService;
		private readonly IExcelImportService _excelImportService;
		public CollectionUnitController(ICollectionUnitService smallCollectionService, IExcelImportService excelImportService)
		{
			_smallCollectionService = smallCollectionService;
			_excelImportService = excelImportService;
		}
		[HttpGet("company/{companyId}")]
		public async Task<IActionResult> GetByCompanyId([FromRoute] string companyId)
		{
			var result = await _smallCollectionService.GetSmallCollectionPointByCompanyId(companyId);
			return Ok(result);
		}
		[HttpGet("{smallCollectionPointId}")]
		public async Task<IActionResult> GetById([FromRoute] string smallCollectionPointId)
		{
			var result = await _smallCollectionService.GetSmallCollectionById(smallCollectionPointId);
			if (result == null)
			{
				return NotFound();
			}
			return Ok(result);
		}
		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportCollectionPoints(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			using var stream = file.OpenReadStream();
			var result = await _excelImportService.ImportAsync(stream, "SmallCollectionPoint");

			if (result.Success)
			{
				return Ok(result);
			}
			else
			{
				return BadRequest(result);
			}
		}

		[HttpGet("filter")]
		public async Task<IActionResult> GetPagedSmallCollectionPoints([FromQuery] SmallCollectionSearchRequest request)
		{
			var model = new SmallCollectionSearchModel
			{
				CompanyId = request.CompanyId,
				Limit = request.Limit,
				Page = request.Page,
				Status = request.Status
			};
			var result = await _smallCollectionService.GetPagedSmallCollectionPointsAsync(model);
			return Ok(result);
		}

		[HttpGet("active")]
		public async Task<IActionResult> GetActiveSmallCollectionPoints()
		{
			var result = await _smallCollectionService.GetSmallCollectionPointActive();
			return Ok(result);
		}
		[HttpPatch("un-active/{collectionUnitId}")]
		public async Task<IActionResult> UnActiveSmallCollectionPoint([FromRoute] string collectionUnitId)
		{
			var result = await _smallCollectionService.UnActiveCollectionUnit(collectionUnitId);
			if (!result)
			{
				return NotFound();
			}
			return Ok(new { message = "Small collection point has been marked as inactive." });
		}
		[HttpPatch("active/{collectionUnitId}")]
		public async Task<IActionResult> ActiveSmallCollectionPoint([FromRoute] string collectionUnitId)
		{
			var result = await _smallCollectionService.ActiveCollectionUnit(collectionUnitId);
			if (!result)
			{
				return NotFound();
			}
			return Ok(new { message = "Small collection point has been marked as active." });
		}


	}

}

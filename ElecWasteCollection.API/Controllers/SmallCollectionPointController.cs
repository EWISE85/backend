using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/small-collection")]
    [ApiController]
    public class SmallCollectionPointController : ControllerBase
    {
        private readonly ISmallCollectionService _smallCollectionService;
		private readonly IExcelImportService _excelImportService;
		public SmallCollectionPointController(ISmallCollectionService smallCollectionService, IExcelImportService excelImportService)
		{
			_smallCollectionService = smallCollectionService;
			_excelImportService = excelImportService;
		}
		[HttpGet("company/{companyId}")]
		public IActionResult GetByCompanyId([FromRoute] string companyId)
		{
			var result = _smallCollectionService.GetSmallCollectionPointByCompanyId(companyId);
			return Ok(result);
		}
		[HttpGet("{smallCollectionPointId}")]
		public IActionResult GetById([FromRoute] string smallCollectionPointId)
		{
			var result = _smallCollectionService.GetSmallCollectionById(smallCollectionPointId);
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

	}

}

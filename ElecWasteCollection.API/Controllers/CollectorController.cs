using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/collectors")]
	[ApiController]
	public class CollectorController : ControllerBase
	{
		private readonly ICollectorService _collectorService;
		private readonly IExcelImportService _excelImportService;
		public CollectorController(ICollectorService collectorService, IExcelImportService excelImportService)
		{
			_collectorService = collectorService;
			_excelImportService = excelImportService;
		}
		[HttpGet]
		public async Task<IActionResult> GetAllCollectors()
		{
			var collectors = await _collectorService.GetAll();
			return Ok(collectors);
		}
		[HttpGet("small-collection-point/{SmallCollectionPointId}")]
		public async Task<IActionResult> GetCollectors([FromRoute] string SmallCollectionPointId)
		{
			var collectors = await _collectorService.GetCollectorByWareHouseId(SmallCollectionPointId);
			return Ok(collectors);
		}
		[HttpGet("{collectorId}")]
		public async Task<IActionResult> GetDetailCollector([FromRoute] Guid collectorId)
		{
			var collectors = await _collectorService.GetById(collectorId);
			return Ok(collectors);
		}
		[HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCollectorsByCompany( string companyId, int page = 1, int limit = 10)
        {
            var result = await _collectorService
                .GetCollectorsByCompanyIdPagedAsync(companyId, page, limit);
            return Ok(result);
        }

        [HttpPost("import-excel")]
		public async Task<IActionResult> ImportCollectors(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			using var stream = file.OpenReadStream();
			var result = await _excelImportService.ImportAsync(stream, "Collector");

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
		public async Task<IActionResult> GetPagedCollectors([FromQuery] CollectorSearchRequest request)
		{
			var model = new CollectorSearchModel
			{
				CompanyId = request.CompanyId,
				SmallCollectionId = request.SmallCollectionId,
				Limit = request.Limit,
				Page = request.Page,
				Status = request.Status
			};
			var result = await _collectorService.GetPagedCollectorsAsync(model);
			return Ok(result);
		}
	}
}

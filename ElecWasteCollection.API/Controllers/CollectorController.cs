using ElecWasteCollection.Application.IServices;
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
		public IActionResult GetAllCollectors()
		{
			var collectors = _collectorService.GetAll();
			return Ok(collectors);
		}
		[HttpGet("small-collection-point/{SmallCollectionPointId}")]
		public IActionResult GetCollectors([FromRoute] int SmallCollectionPointId)
		{
			var collectors = _collectorService.GetCollectorByWareHouseId(SmallCollectionPointId);
			return Ok(collectors);
		}
		[HttpGet("{collectorId}")]
		public IActionResult GetDetailCollector([FromRoute] Guid collectorId)
		{
			var collectors = _collectorService.GetById(collectorId);
			return Ok(collectors);
		}
		[HttpGet("company/{companyId}")]
		public IActionResult GetCollectorsByCompany([FromRoute] int companyId)
		{
			var collectors = _collectorService.GetCollectorByCompanyId(companyId);
			return Ok(collectors);
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
	}
}

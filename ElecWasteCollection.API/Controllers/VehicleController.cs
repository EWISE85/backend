using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/vehicle")]
	[ApiController]
	public class VehicleController : ControllerBase
	{
		private readonly IVehicleService _vehicleService;
		private readonly IExcelImportService _excelImportService;
		public VehicleController(IVehicleService vehicleService, IExcelImportService excelImportService)
		{
			_vehicleService = vehicleService;
			_excelImportService = excelImportService;
		}
		[HttpGet("{vehicleId}")]
		public async Task<IActionResult> GetVehicleById(string vehicleId)
		{
			var vehicle =  await _vehicleService.GetVehicleById(vehicleId);
			if (vehicle == null)
			{
				return NotFound();
			}
			return Ok(vehicle);
		}
		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportVehiclesFromExcel(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}
			using var stream = file.OpenReadStream();
			var importResult = await _excelImportService.ImportAsync(stream, "Vehicle");
			if (!importResult.Success)
			{
				return BadRequest(importResult);
			}
			return Ok(importResult);
		}
		[HttpGet("filter")]
		public async Task<IActionResult> GetPagedVehicles([FromQuery] VehicleSearchRequest request)
		{
			var model = new VehicleSearchModel
			{
				Page = request.Page,
				Limit = request.Limit,
				CollectionCompanyId = request.CollectionCompanyId,
				SmallCollectionPointId = request.SmallCollectionPointId,
				PlateNumber = request.PlateNumber,
				Status = request.Status
			};
			var result = await _vehicleService.PagedVehicles(model);
			return Ok(result);
		}
	}
}

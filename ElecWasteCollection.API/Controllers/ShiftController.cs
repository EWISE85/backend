using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/shift")]
    [ApiController]
    public class ShiftController : ControllerBase
    {
		private readonly IShiftService _shiftService;
		private readonly IExcelImportService _excelImportService;

		public ShiftController(IShiftService shiftService, IExcelImportService excelImportService)
		{
			_shiftService = shiftService;
			_excelImportService = excelImportService;
		}
		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportShiftsFromExcel(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			using var stream = file.OpenReadStream();
			var result = await _excelImportService.ImportAsync(stream, "Shift");

			if (!result.Success)
			{
				return BadRequest(result);
			}

			return Ok(result);
		}
		[HttpGet("filter")]
		public async Task<IActionResult> GetPagedShifts([FromQuery] ShiftSearchRequest request)
		{
			var model = new ShiftSearchModel
			{
				Page = request.Page,
				Limit = request.Limit,
				FromDate = request.FromDate,
				ToDate = request.ToDate,
				CollectionCompanyId = request.CollectionCompanyId,
				SmallCollectionPointId = request.SmallCollectionPointId,
				Status = request.Status
			};
			var result = await _shiftService.GetPagedShiftAsync(model);
			return Ok(result);
		}
		[HttpGet("{id}")]
		public IActionResult GetShiftById([FromRoute] string id)
		{
			var shift = _shiftService.GetShiftById(id);
			if (shift == null)
			{
				return NotFound();
			}
			return Ok(shift);
		}

	}
}

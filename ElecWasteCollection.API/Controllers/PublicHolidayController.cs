using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Ocsp;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/holiday")]
    [ApiController]
    public class PublicHolidayController : ControllerBase
    {
        private readonly IPublicHolidayService _publicHolidayService;
		private readonly IExcelImportService _excelImportService;

		public PublicHolidayController(IPublicHolidayService publicHolidayService, IExcelImportService excelImportService)
		{
			_publicHolidayService = publicHolidayService;
			_excelImportService = excelImportService;
		}

		[HttpGet("active")]
		public async Task<IActionResult> GetAllActivePublicHolidays()
		{
			var holidays = await _publicHolidayService.GetAllPublicHolidayActive();
			return Ok(holidays);
		}
		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportPublicHolidaysFromExcel(IFormFile file)
		{
			try
			{
				using var stream = file.OpenReadStream();
				var result = await _excelImportService.ImportAsync(stream, "PublicHoliday");
				if (result.Success)
				{
					return Ok(new { Message = "Import vouchers thành công.", Details = result.Messages });
				}
				else
				{
					return BadRequest(new { Message = "Import vouchers thất bại.", Details = result.Messages });
				}
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { Message = "Lỗi khi import vouchers từ Excel.", Error = ex.Message });
			}
		}
	}
}

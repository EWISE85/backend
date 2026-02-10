using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/QR")]
	[ApiController]
	public class QrController : ControllerBase
	{
		private readonly ICompanyQrService _companyQrService;
		public QrController(ICompanyQrService companyQrService)
		{
			_companyQrService = companyQrService;
		}
		[HttpGet("Generate/{companyId}")]
		public IActionResult GenerateQrCode(string companyId)
		{
			var qrCode = _companyQrService.GenerateQrCode(companyId);
			return Ok(new { QrCode = qrCode });
		}
		[HttpPost("Verify/{qrCode}")]
		public async Task<IActionResult> VerifyQrCode(string qrCode)
		{
			var company = await _companyQrService.VerifyQrCodeAsync(qrCode);
			if (company == null)
			{
				return NotFound(new { Message = "Invalid or expired QR code." });
			}
			return Ok(company);
		}
	}
}

using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
	[Route("api/collection-company")]
	[ApiController]
	public class CollectionCompanyController : ControllerBase
	{
		private readonly IExcelImportService _excelImportService;
		private readonly ICompanyService _collectionCompanyService;
		public CollectionCompanyController(
			IExcelImportService excelImportService,
			ICompanyService collectionCompanyService)
		{
			_excelImportService = excelImportService;
			_collectionCompanyService = collectionCompanyService;
		}
		[HttpPost("import-excel")]
		public async Task<IActionResult> ImportCollectionCompanies(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				return BadRequest("No file uploaded.");
			}

			using var stream = file.OpenReadStream();
			var result = await _excelImportService.ImportAsync(stream, "Company");

			if (result.Success)
			{
				return Ok(result);
			}
			else
			{
				return BadRequest(result);
			}
		}
		[HttpGet()]
		public async Task<IActionResult> GetAllCollectionCompanies()
		{
			var result = await _collectionCompanyService.GetAllCollectionCompaniesAsync();
			return Ok(result);
		}
		[HttpGet("{companyId}")]
		public  async Task<IActionResult> GetCollectionCompanyById([FromRoute] string companyId)
		{
			var result = await _collectionCompanyService.GetCompanyById(companyId);
			if (result == null)
			{
				return NotFound();
			}
			return Ok(result);
		}
		[HttpGet("filter")]
		public async Task<IActionResult> GetPagedCollectionCompanies([FromQuery] CompanySearchRequest request)
		{
			var model = new CompanySearchModel
			{
				Limit = request.Limit,
				Page = request.Page,
				Status = request.Status
			};
			var result = await _collectionCompanyService.GetPagedCompanyAsync(model);
			return Ok(result);
		}
		[HttpGet("name")]
		public async Task<IActionResult> GetCollectionCompanyByName([FromQuery] string name)
		{
			var result = await _collectionCompanyService.GetCompanyByName(name);
			if (result == null || result.Count == 0)
			{
				return NotFound();
			}
			return Ok(result);
		}
	}
}

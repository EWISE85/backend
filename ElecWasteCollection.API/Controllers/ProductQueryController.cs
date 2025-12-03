using ElecWasteCollection.Application.IServices.IAssignPost;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{

    [ApiController]
    [Route("api/product-query")]
    public class ProductQueryController : ControllerBase
    {
        private readonly IProductQueryService _productQueryService;

        public ProductQueryController(IProductQueryService productQueryService)
        {
            _productQueryService = productQueryService;
        }

        [HttpGet("company/{companyId}")]
        public async Task<IActionResult> GetCompanyProducts(
            int companyId,
            [FromQuery] string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return BadRequest("workDate không hợp lệ. Định dạng yyyy-MM-dd");

            var result = await _productQueryService.GetCompanyProductsAsync(companyId, date);
            return Ok(result);
        }

        [HttpGet("small-point/{smallPointId}")]
        public async Task<IActionResult> GetSmallPointProducts(
            int smallPointId,
            [FromQuery] string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return BadRequest("workDate không hợp lệ. Định dạng yyyy-MM-dd");

            var result = await _productQueryService.GetSmallPointProductsAsync(smallPointId, date);
            return Ok(result);
        }

        [HttpGet("companies-with-points")]
        public async Task<IActionResult> GetCompaniesWithPoints()
        {
            var result = await _productQueryService.GetCompaniesWithSmallPointsAsync();
            return Ok(result);
        }

        [HttpGet("{companyId}/smallpoints")]
        public async Task<IActionResult> GetSmallPoints(int companyId)
        {
            var result = await _productQueryService.GetSmallPointsByCompanyIdAsync(companyId);
            return Ok(result);
        }

        [HttpGet("config/company/{companyId}")]
        public async Task<IActionResult> GetCompanyConfigById(int companyId)
        {
            var result = await _productQueryService.GetCompanyConfigByCompanyIdAsync(companyId);
            return Ok(result);
        }
    }

}

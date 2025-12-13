using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [ApiController]
    [Route("api/assign")]
    public class AssignController : ControllerBase
    {
        private readonly ICompanyConfigService _companyConfigService;
        private readonly IProductAssignService _productAssignService;

        public AssignController(
            ICompanyConfigService companyConfigService,
            IProductAssignService productAssignService)
        {
            _companyConfigService = companyConfigService;
            _productAssignService = productAssignService;
        }

        [HttpPost("company-config")]
        public async Task<IActionResult> UpdateCompanyConfig([FromBody] CompanyConfigRequest request)
        {
            var result = await _companyConfigService.UpdateCompanyConfigAsync(request);
            if (result.Companies == null || !result.Companies.Any())
            {
                return Ok(result);
            }
            return Ok(result);
        }

        [HttpGet("company-config")]
        public async Task<IActionResult> GetCompanyConfig()
        {
            var result = await _companyConfigService.GetCompanyConfigAsync();
            return Ok(result);
        }


        [HttpPost("products")]
        public async Task<IActionResult> AssignProducts([FromBody] AssignProductRequest request)
        {
            if (request == null)
                return BadRequest("Request cannot be null.");

            if (request.ProductIds == null || !request.ProductIds.Any())
                return BadRequest("ProductIds cannot be empty.");

            if (!DateOnly.TryParse(request.WorkDate, out var workDate))
                return BadRequest("WorkDate không hợp lệ. Hãy nhập yyyy-MM-dd.");

            var result = await _productAssignService.AssignProductsAsync(request.ProductIds, workDate);
            return Ok(result);
        }

        [HttpGet("products-by-date")]
        public async Task<IActionResult> GetProductsByDate([FromQuery] string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return BadRequest("workDate không hợp lệ. Định dạng yyyy-MM-dd");

            var result = await _productAssignService.GetProductsByWorkDateAsync(date);
            return Ok(result);
        }
    }
}

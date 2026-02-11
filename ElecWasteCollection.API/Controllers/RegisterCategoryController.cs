using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompanyCategoriesController : ControllerBase
    {
        private readonly IRegisterCategoryService _registerCategoryService;

        public CompanyCategoriesController(IRegisterCategoryService registerCategoryService)
        {
            _registerCategoryService = registerCategoryService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterCategoryRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.CompanyId))
                return BadRequest("Dữ liệu không hợp lệ.");

            var result = await _registerCategoryService.RegisterRecyclingCategoriesAsync(request);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("{companyId}")]
        public async Task<IActionResult> GetRegistered(string companyId)
        {
            var categoryIds = await _registerCategoryService.GetRegisteredCategoryIdsAsync(companyId);
            return Ok(categoryIds);
        }
        [HttpGet("recycling-companies")]
        public async Task<IActionResult> GetAllCompanies([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _registerCategoryService.GetAllRecyclingCompaniesAsync(pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

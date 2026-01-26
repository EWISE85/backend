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
            string companyId,
            [FromQuery] string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return BadRequest("workDate không hợp lệ. Định dạng yyyy-MM-dd");

            var result = await _productQueryService.GetCompanyProductsAsync(companyId, date);
            return Ok(result);
        }

        [HttpGet("small-point/{smallPointId}")]
        public async Task<IActionResult>
        GetProductsPaged(
            string smallPointId,
            [FromQuery] DateOnly workDate,
            [FromQuery] int page = 1,
            [FromQuery] int limit = 10)
        {
            var result =
                await _productQueryService
                    .GetSmallPointProductsPagedAsync(
                        smallPointId,
                        workDate,
                        page,
                        limit);

            return Ok(result);
        }

        [HttpGet("companies-with-points")]
        public async Task<IActionResult> GetCompaniesWithPoints()
        {
            var result = await _productQueryService.GetCompaniesWithSmallPointsAsync();
            return Ok(result);
        }

        [HttpGet("{companyId}/smallpoints")]
        public async Task<IActionResult> GetSmallPoints(string companyId)
        {
            var result = await _productQueryService.GetSmallPointsByCompanyIdAsync(companyId);
            return Ok(result);
        }
        [HttpGet("daily-summary")]
        public async Task<IActionResult> GetCompanySummaries([FromQuery] DateOnly workDate)
        {
            try
            {
                if (workDate == DateOnly.MinValue)
                {
                    workDate = DateOnly.FromDateTime(DateTime.Now);
                }

                var result = await _productQueryService.GetCompanySummariesByDateAsync(workDate);

                return Ok(new
                {
                    Success = true,
                    Message = "Lấy dữ liệu thành công",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
        }

        [HttpGet("config/company/{companyId}")]
        public async Task<IActionResult> GetCompanyConfigById(string companyId)
        {
            var result = await _productQueryService.GetCompanyConfigByCompanyIdAsync(companyId);
            return Ok(result);
        }
        [HttpGet("small-point/product-ids")]
        public async Task<IActionResult> GetIdsAtPoint([FromQuery] string smallPointId, [FromQuery] DateOnly workDate)
        {
            var result = await _productQueryService.GetProductIdsAtSmallPointAsync(smallPointId, workDate);
            return Ok(result);
        }

        [HttpGet("company-metrics")]
        public async Task<IActionResult> GetCompanyMetrics([FromQuery] DateOnly workDate)
        {
            var result = await _productQueryService.GetAllCompaniesDailyMetricsAsync(workDate);
            return Ok(result);
        }
    }

}

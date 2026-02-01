using ElecWasteCollection.API.DTOs.Request;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using System.Security.Claims;

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
        //[RequestTimeout(600000)]
        public  IActionResult AssignProducts([FromBody] AssignProductRequest request)
        {
            if (request == null) return BadRequest("Request không được để trống.");
            if (request.ProductIds == null || !request.ProductIds.Any())
                return BadRequest("Danh sách ProductIds không được để trống.");
            if (!DateOnly.TryParse(request.WorkDate, out var workDate))
                return BadRequest("WorkDate không hợp lệ. Định dạng yêu cầu là yyyy-MM-dd.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("Không xác định được danh tính người dùng.");
            }

            try
            {
                _productAssignService.AssignProductsInBackground(
                    request.ProductIds,
                    workDate,
                    userId,
                    request.TargetCompanyIds
                );

                return Accepted(new
                {
                    Success = true,
                    Message = "Hệ thống đang xử lý phân bổ ngầm cho các đơn vị được chọn. Vui lòng đợi thông báo kết quả...",
                    IsProcessingInBackground = true
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

            //         if (request == null) return BadRequest("Request cannot be null.");
            //         if (request.ProductIds == null || !request.ProductIds.Any()) return BadRequest("ProductIds cannot be empty.");
            //         if (!DateOnly.TryParse(request.WorkDate, out var workDate)) return BadRequest("WorkDate không hợp lệ. Hãy nhập yyyy-MM-dd.");
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            //if (string.IsNullOrEmpty(userId))
            //{
            //	return Unauthorized("Không xác định được danh tính người dùng.");
            //}

            //try
            //         {
            //	_productAssignService.AssignProductsInBackground(request.ProductIds, workDate, userId);
            //	return Accepted(new
            //	{
            //		Success = true,
            //		Message = "Hệ thống đang xử lý phân bổ ngầm. Vui lòng đợi thông báo kết quả...",
            //		IsProcessingInBackground = true
            //	});
            //}
            //         catch (Exception ex)
            //         {
            //             return BadRequest(new { Message = ex.Message });
            //         }
        }

        [HttpGet("products-by-date")]
        public async Task<IActionResult> GetProductsByDate([FromQuery] string workDate)
        {
            if (!DateOnly.TryParse(workDate, out var date))
                return BadRequest("WorkDate không hợp lệ. Định dạng yyyy-MM-dd");

            var result = await _productAssignService.GetProductsByWorkDateAsync(date);

            if (result == null || !result.Any())
            {
                return Ok(new
                {
                    WorkDate = date.ToString("yyyy-MM-dd"),
                    Message = $"Chưa có sản phẩm nào cần gom nhóm cho ngày {date:yyyy-MM-dd}."
                });
            }

            return Ok(result);
        }

        [HttpGet("ids")]
        public async Task<IActionResult> GetProductIdsByDate([FromQuery] DateOnly date)
        {
            try
            {
                var result = await _productAssignService.GetProductIdsForWorkDateAsync(date);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}

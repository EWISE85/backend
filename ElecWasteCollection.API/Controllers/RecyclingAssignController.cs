using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace ElecWasteCollection.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecyclingAssignController : ControllerBase
    {
        private readonly IPackageAssignService _service;

        public RecyclingAssignController(IPackageAssignService service)
        {
            _service = service;
        }

        [HttpGet("recycling-companies")]
        public async Task<IActionResult> GetRecyclingCompanies()
        {
            try
            {
                var result = await _service.GetRecyclingCompaniesAsync();

                if (result == null || !result.Any())
                {
                    return Ok(new { message = "Không tìm thấy công ty tái chế nào.", data = result });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("assign-scp")]
        public async Task<IActionResult> AssignScpToCompany([FromBody] List<AssignScpToCompanyRequest> requests)
        {
            if (requests == null || !requests.Any())
                return BadRequest(new { message = "Danh sách yêu cầu không được để trống." });

            try
            {
                await _service.AssignScpToCompanyAsync(requests);
                return Ok(new { message = $"Đã xử lý thành công {requests.Count} nhóm yêu cầu phân bổ." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("scp/{scpId}/assign")]
        public async Task<IActionResult> UpdateScpAssignment(string scpId, [FromBody] UpdateScpAssignmentRequest request)
        {
            try
            {
                await _service.UpdateScpAssignmentAsync(scpId, request.NewRecyclingCompanyId);
                return Ok(new { message = "Cập nhật đơn vị tái chế thành công." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("list-scp")]
        public async Task<IActionResult> GetAssignmentDashboard()
        {
            try
            {
                var result = await _service.GetAssignmentOverviewAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("scp-assignment-detail/{companyId}")]
        public async Task<IActionResult> GetScpAssignmentDetail(string companyId)
        {
            if (string.IsNullOrEmpty(companyId))
                return BadRequest(new { message = "Vui lòng truyền ID công ty thu gom." });

            try
            {
                var result = await _service.GetScpAssignmentDetailAsync(companyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}
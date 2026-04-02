using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CapacityController : ControllerBase
{
    private readonly ICapacityService _capacityService;

    public CapacityController(ICapacityService capacityService)
    {
        _capacityService = capacityService;
    }

    [HttpGet("points")]
    public async Task<IActionResult> GetAll()
        => Ok(await _capacityService.GetAllSCPCapacityAsync());

    [HttpGet("company/{companyId}")]
    public async Task<IActionResult> GetByCompany(string companyId)
        => Ok(await _capacityService.GetCompanyCapacitySummaryAsync(companyId));

    [HttpGet("company/Date/{companyId}")]
    public async Task<IActionResult> GetCompanyCapacityByDate(string companyId, [FromQuery] DateOnly date)
    {
        try
        {
            var result = await _capacityService.GetCompanyCapacityByDateAsync(companyId, date);

            if (result == null)
            {
                return NotFound(new { message = "Không tìm thấy dữ liệu cho công ty này." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Đã xảy ra lỗi hệ thống.", detail = ex.Message });
        }
    }
}
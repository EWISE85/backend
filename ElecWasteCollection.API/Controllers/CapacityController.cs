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
}
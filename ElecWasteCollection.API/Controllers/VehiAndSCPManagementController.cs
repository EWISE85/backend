using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/management")]
public class VehiAndSCPManagementController : ControllerBase
{
    private readonly IVehiAndSCPManagementService _managementService;

    public VehiAndSCPManagementController(IVehiAndSCPManagementService managementService)
    {
        _managementService = managementService;
    }

    [HttpPatch("vehicles/{id}/approve")]
    public async Task<IActionResult> ApproveVehicle(string id)
    {
        await _managementService.ApproveVehicleAsync(id);
        return Ok(new { message = "Xe đã được chuyển sang trạng thái hoạt động." });
    }

    [HttpPatch("vehicles/{id}/block")]
    public async Task<IActionResult> BlockVehicle(string id)
    {
        await _managementService.BlockVehicleAsync(id);
        return Ok(new { message = "Xe đã bị khóa." });
    }

    [HttpPatch("collection-points/{id}/approve")]
    public async Task<IActionResult> ApprovePoint(string id)
    {
        await _managementService.ApproveSmallCollectionPointAsync(id);
        return Ok(new { message = "Điểm thu gom đã được chuyển sang trạng thái hoạt động." });
    }

    [HttpPatch("collection-points/{id}/block")]
    public async Task<IActionResult> BlockPoint(string id)
    {
        await _managementService.BlockSmallCollectionPointAsync(id);
        return Ok(new { message = "Điểm thu gom đã bị khóa." });
    }
}
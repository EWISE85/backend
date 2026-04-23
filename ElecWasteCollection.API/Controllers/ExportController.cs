using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExportService _exportService;

    public ExportController(IExportService exportService)
    {
        _exportService = exportService;
    }

    [HttpGet("full-system")]
    public async Task<IActionResult> DownloadExcel([FromQuery] DateOnly from, [FromQuery] DateOnly to)
    {
        var content = await _exportService.ExportFullSystemDashboardAsync(from, to);
        var fileName = $"EWISE_Report_Brand_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx";

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
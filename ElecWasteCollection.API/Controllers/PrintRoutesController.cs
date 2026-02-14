using ElecWasteCollection.Application.IServices;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PrintRoutesController : ControllerBase
{
    private readonly IPrintService _printService;

    public PrintRoutesController(IPrintService printService)
    {
        _printService = printService;
    }

    [HttpGet("export-pdf/{groupId}")]
    public async Task<IActionResult> ExportPdf(int groupId)
    {
        try
        {
            var pdfBytes = await _printService.GenerateCollectionPdfByGroupIdAsync(groupId);

            return File(pdfBytes, "application/pdf", $"Danh_Sach_Thu_Gom_So_{groupId}.pdf");
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
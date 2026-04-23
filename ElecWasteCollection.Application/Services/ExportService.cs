
using System.Drawing;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Application.IServices;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ElecWasteCollection.Application.Services
{
    public class ExportService : IExportService
    {
        private readonly IDashboardRepository _repo;

        public ExportService(IDashboardRepository repo)
        {
            _repo = repo;
        }

        public async Task<byte[]> ExportFullSystemDashboardAsync(DateOnly from, DateOnly to)
        {
            var data = await _repo.GetExportDataRawAsync(from, to);

            OfficeOpenXml.ExcelPackage.License.SetNonCommercialPersonal("EWISE Project");
            using var package = new ExcelPackage();

            var ws = package.Workbook.Worksheets.Add("Báo cáo chi tiết Brand");

            ws.Cells["A1:H1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO CHI TIẾT THU GOM THEO THƯƠNG HIỆU";
            ws.Cells["A1"].Style.Font.Size = 18;
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Color.SetColor(Color.FromArgb(31, 78, 120));
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ws.Cells["A2:H2"].Merge = true;
            ws.Cells["A2"].Value = $"Thời gian: Từ ngày {from:dd/MM/yyyy} - Đến ngày {to:dd/MM/yyyy}";
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A2"].Style.Font.Italic = true;

            string[] headers = { "STT", "Người đóng góp", "Loại sản phẩm", "Điểm", "Ngày thu gom", "Điểm thu gom" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[4, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(46, 117, 182));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
            }

            var groupedData = data.GroupBy(x => x.BrandName).OrderBy(g => g.Key);
            int currentRow = 5;
            int stt = 1;

            foreach (var brandGroup in groupedData)
            {
                ws.Cells[currentRow, 1, currentRow, 8].Merge = true;
                ws.Cells[currentRow, 1].Value = $"THƯƠNG HIỆU: {brandGroup.Key.ToUpper()} ({brandGroup.Count()} sản phẩm)";
                ws.Cells[currentRow, 1].Style.Font.Bold = true;
                ws.Cells[currentRow, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[currentRow, 1].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(221, 235, 247));
                ws.Cells[currentRow, 1].Style.Border.BorderAround(ExcelBorderStyle.Thin);

                currentRow++;

                foreach (var item in brandGroup)
                {
                    ws.Cells[currentRow, 1].Value = stt++;
                    ws.Cells[currentRow, 2].Value = item.UserName;
                    ws.Cells[currentRow, 3].Value = item.CategoryName;
                    ws.Cells[currentRow, 4].Value = item.Point;
                    ws.Cells[currentRow, 4].Style.Numberformat.Format = "#,##0";
                    ws.Cells[currentRow, 5].Value = item.CollectedDate?.ToString("dd/MM/yyyy");
                    ws.Cells[currentRow, 6].Value = item.ScpName;
                    ws.Cells[currentRow, 1, currentRow, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
                    currentRow++;
                }
                ws.Cells[currentRow - 1, 1, currentRow - 1, 8].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            ws.Cells.AutoFitColumns();
            ws.View.FreezePanes(5, 1);

            return await package.GetAsByteArrayAsync();
        }
    }
}
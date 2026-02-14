using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.IRepository;
using SkiaSharp;
using Color = QuestPDF.Infrastructure.Color;

namespace ElecWasteCollection.Application.Services
{
    public class PrintService : IPrintService
    {
        private readonly IUnitOfWork _unitOfWork;

        public PrintService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<byte[]> GenerateCollectionPdfByGroupIdAsync(int groupId)
        {
            var group = await _unitOfWork.CollectionGroupGeneric.GetByIdAsync(groupId)
                         ?? throw new Exception("Không tìm thấy group.");

            var shift = await _unitOfWork.Shifts.GetByIdAsync(group.Shift_Id);
            var allRoutes = await _unitOfWork.CollecctionRoutes.GetAllAsync(r => r.CollectionGroupId == groupId);

            var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(shift.Vehicle_Id);
            var collector = await _unitOfWork.Users.GetByIdAsync(shift.CollectorId);
            var pointId = vehicle?.Small_Collection_Point ?? collector?.SmallCollectionPointId;
            var point = await _unitOfWork.SmallCollectionPoints.GetByIdAsync(pointId);

            var routeDtos = new List<RouteDto>();
            int order = 1;

            foreach (var r in allRoutes.OrderBy(x => x.EstimatedTime))
            {
                var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == r.ProductId);
                if (post == null) continue;

                var product = post.Product ?? await _unitOfWork.Products.GetByIdAsync(r.ProductId);
                var category = await _unitOfWork.Categories.GetByIdAsync(product.CategoryId);
                var brand = await _unitOfWork.Brands.GetByIdAsync(product.BrandId);

                routeDtos.Add(new RouteDto
                {
                    PickupOrder = order++,
                    Address = post.Address ?? "N/A",
                    CategoryName = category?.Name ?? "N/A",
                    BrandName = brand?.Name ?? "N/A",
                    EstimatedArrival = r.EstimatedTime.ToString("HH:mm")
                });
            }

            var dataDto = new CollectionGroupDto
            {
                GroupCode = group.Group_Code,
                Vehicle = vehicle != null ? $"{vehicle.Plate_Number}" : "N/A",
                Collector = collector?.Name ?? "N/A",
                GroupDate = shift.WorkDate.ToString("dd/MM/yyyy"),
                CollectionPoint = point?.Name ?? "N/A",
                Routes = routeDtos
            };

            return GenerateCollectionPdf(dataDto);
        }

        public byte[] GenerateCollectionPdf(CollectionGroupDto data)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            string checkboxSvg = @"<svg width='14' height='14' viewBox='0 0 14 14' fill='none' xmlns='http://www.w3.org/2000/svg'>
                            <rect x='0.5' y='0.5' width='13' height='13' rx='1.5' stroke='#9E9E9E'/>
                          </svg>";

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text("DANH SÁCH THU GOM").FontSize(26).ExtraBold().FontColor("#1A237E");

                        col.Item().PaddingTop(5).Row(row =>
                        {
                            row.RelativeItem().Text(t => {
                                t.Span("Mã nhóm: ").SemiBold();
                                t.Span(data.GroupCode).FontColor("#E65100");
                            });
                            row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(9).Italic().FontColor(Colors.Grey.Medium);
                        });
                        col.Item().PaddingVertical(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().Column(mainCol =>
                    {
                        mainCol.Item().Row(row =>
                        {
                            row.RelativeItem().PaddingRight(5).Background("#F5F5FB").Padding(10).Column(c => {
                                c.Item().Text("XE").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(data.Vehicle).Bold().FontSize(11);
                            });
                            row.RelativeItem().PaddingHorizontal(5).Background("#F5F5FB").Padding(10).Column(c => {
                                c.Item().Text("NHÂN VIÊN").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                                c.Item().Text(data.Collector).Bold().FontSize(11);
                            });
                            row.RelativeItem().PaddingLeft(5).Background("#FFF3E0").Padding(10).Column(c => {
                                c.Item().Text("ĐIỂM TẬP KẾT").FontSize(8).SemiBold().FontColor("#E65100");
                                c.Item().Text(data.CollectionPoint).Bold().FontSize(11).FontColor("#E65100");
                            });
                        });

                        mainCol.Item().PaddingTop(20).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(35);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(5);
                                columns.RelativeColumn(2);
                                columns.ConstantColumn(40);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(CellStyle).Text("STT");
                                header.Cell().Element(CellStyle).Text("SẢN PHẨM");
                                header.Cell().Element(CellStyle).Text("ĐỊA CHỈ");
                                header.Cell().Element(CellStyle).AlignRight().Text("GIỜ");
                                header.Cell().Element(CellStyle).AlignCenter().Text("XN");

                                IContainer CellStyle(IContainer c) => c.Background("#1A237E").Padding(8).DefaultTextStyle(x => x.SemiBold().FontColor(Colors.White).FontSize(9));
                            });

                            int rowIndex = 0;
                            foreach (var route in data.Routes)
                            {
                                var bgColor = (rowIndex % 2 == 0) ? (QuestPDF.Infrastructure.Color)Colors.White : (QuestPDF.Infrastructure.Color)"#F9F9F9";

                                table.Cell().Element(ContentStyle).AlignCenter().Text(route.PickupOrder.ToString());
                                table.Cell().Element(ContentStyle).Text($"{route.CategoryName} - {route.BrandName}").Bold();
                                table.Cell().Element(ContentStyle).Text(route.Address).FontSize(9);
                                table.Cell().Element(ContentStyle).AlignRight().Text(route.EstimatedArrival).Bold().FontColor("#1A237E");
                                table.Cell().Element(ContentStyle).AlignCenter().AlignMiddle().Width(14).Height(14).Svg(checkboxSvg);

                                IContainer ContentStyle(IContainer c) => c.Background(bgColor).BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(10).AlignMiddle();
                                rowIndex++;
                            }
                        });

                        mainCol.Item().PaddingTop(40).Row(row =>
                        {
                            row.RelativeItem().PaddingLeft(20).Column(c => {
                                c.Item().Text("Người lập lệnh").SemiBold();
                                c.Item().PaddingTop(40).Text("........................").FontColor(Colors.Grey.Lighten2);
                            });
                            row.RelativeItem().AlignRight().PaddingRight(20).Column(c => {
                                c.Item().AlignRight().Text("Nhân viên thu gom").SemiBold();
                                c.Item().PaddingTop(40).AlignRight().Text("........................").FontColor(Colors.Grey.Lighten2);
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x => {
                        x.Span("Trang ").FontSize(9);
                        x.CurrentPageNumber().FontSize(9);
                        x.Span(" / ").FontSize(9);
                        x.TotalPages().FontSize(9);
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
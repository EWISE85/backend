using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;

namespace ElecWasteCollection.Application.Services
{
	public class VehicleService : IVehicleService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IVehicleRepository _vehicleRepository;
		private readonly ICollectionUnitRepository _smallCollectionRepository;
		public VehicleService(IUnitOfWork unitOfWork, IVehicleRepository vehicleRepository, ICollectionUnitRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_vehicleRepository = vehicleRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}
		public async Task<ImportResult> CheckAndUpdateVehicleAsync(CreateVehicleModel vehicle)
		{
			var importResult = new ImportResult();
			var existingVehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicle.VehicleId);
			if (existingVehicle != null)
			{
				existingVehicle.Plate_Number = vehicle.Plate_Number;
				existingVehicle.Vehicle_Type = vehicle.Vehicle_Type;
				existingVehicle.Capacity_Kg = vehicle.Capacity_Kg;
                existingVehicle.Length_M = vehicle.Length_M;
                existingVehicle.Width_M = vehicle.Width_M;
                existingVehicle.Height_M = vehicle.Height_M; 
				existingVehicle.Status = vehicle.Status;
				existingVehicle.CollectionUnit = vehicle.Small_Collection_Point;
				 _unitOfWork.Vehicles.Update(existingVehicle);
			}
			else
			{
				var newVehicle = new Vehicles
				{
					VehicleId = vehicle.VehicleId,
					Plate_Number = vehicle.Plate_Number,
					Vehicle_Type = vehicle.Vehicle_Type,
					Capacity_Kg = vehicle.Capacity_Kg,
                    Length_M = vehicle.Length_M,
                    Width_M = vehicle.Width_M,
                    Height_M = vehicle.Height_M,
                    Status = vehicle.Status,
					CollectionUnit = vehicle.Small_Collection_Point
				};
				await _unitOfWork.Vehicles.AddAsync(newVehicle);

			}
			await _unitOfWork.SaveAsync();
			return importResult;
		}

	public async Task<VehicleModel?> GetVehicleById(string vehicleId)
		{
			var vehicle = await _vehicleRepository.GetAsync(v => v.VehicleId == vehicleId);
			if (vehicle == null)
			{
				throw new AppException("Xe không tồn tại", 404);
			}
			var smallCollectionPoint = await _smallCollectionRepository.GetAsync(scp => scp.CollectionUnitId == vehicle.CollectionUnit);
			return new VehicleModel
			{
				VehicleId = vehicle.VehicleId,
				PlateNumber = vehicle.Plate_Number,
				VehicleType = vehicle.Vehicle_Type,
				CapacityKg = vehicle.Capacity_Kg,
                LengthM = vehicle.Length_M,
                WidthM = vehicle.Width_M,
                HeightM = vehicle.Height_M,
                Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(vehicle.Status),
                SmallCollectionPointId = vehicle.CollectionUnit,
				SmallCollectionPointName = smallCollectionPoint?.Name ?? "Chưa gán điểm thu gom"
			};

		}

		public async Task<PagedResultModel<VehicleModel>> PagedVehicles(VehicleSearchModel model)
		{
			var statusEnum = string.IsNullOrEmpty(model.Status)
				? null
				: StatusEnumHelper.GetValueFromDescription<VehicleStatus>(model.Status).ToString();

			var (vehicles, totalItems) = await _vehicleRepository.GetPagedVehiclesAsync(
				collectionCompanyId: model.CollectionCompanyId,
				smallCollectionPointId: model.SmallCollectionPointId,
				plateNumber: model.PlateNumber,
				status: statusEnum,
				page: model.Page,
				limit: model.Limit
			);


			var scpIds = vehicles
				.Where(v => v.CollectionUnit != null)
				.Select(v => v.CollectionUnit)
				.Distinct()
				.ToList();

			var scpDict = new Dictionary<string, string>();
			if (scpIds.Any())
			{
				// Giả sử bạn có _scpRepository
				var scps = await _smallCollectionRepository.GetsAsync(s => scpIds.Contains(s.CollectionUnitId));
				scpDict = scps.ToDictionary(k => k.CollectionUnitId, v => v.Name);
			}

			var resultList = vehicles.Select(v =>
			{
				string scpName = "Chưa gán điểm thu gom";
				if (v.CollectionUnit != null && scpDict.ContainsKey(v.CollectionUnit))
				{
					scpName = scpDict[v.CollectionUnit];
				}

				return new VehicleModel
				{
					VehicleId = v.VehicleId.ToString(),
					PlateNumber = v.Plate_Number,
					VehicleType = v.Vehicle_Type,
					CapacityKg = v.Capacity_Kg,
                    LengthM = v.Length_M,
                    WidthM = v.Width_M,
                    HeightM = v.Height_M,
                    Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<VehicleStatus>(v.Status),
                    SmallCollectionPointId = v.CollectionUnit,
					SmallCollectionPointName = scpName
				};
			}).ToList();

			return new PagedResultModel<VehicleModel>(resultList, model.Page, model.Limit, totalItems);
		}
		public async Task<byte[]> ExportVehiclesToExcelAsync(string collectionUnitId)
		{
			var vehicles = await _unitOfWork.Vehicles.GetsAsync(v => v.CollectionUnit == collectionUnitId);

			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Vehicles");

				string[] headers = {
			"STT", "Mã phương tiện", "Biển số xe", "Loại xe",
			"Tải trọng (kg)", "Chiều dài (m)", "Chiều rộng (m)",
			"Chiều cao (m)", "Mã đơn vị thu gom", "Tình trạng"
		};

				for (int i = 0; i < headers.Length; i++)
				{
					var cell = worksheet.Cell(1, i + 1);
					cell.Value = headers[i];
					cell.Style.Font.Bold = true;
					cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6");
					cell.Style.Font.FontColor = XLColor.White;
					cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				}

				int currentRow = 2;
				int stt = 1;
				foreach (var vehicle in vehicles)
				{
					worksheet.Cell(currentRow, 1).Value = stt++;
					worksheet.Cell(currentRow, 2).Value = vehicle.VehicleId;
					worksheet.Cell(currentRow, 3).Value = vehicle.Plate_Number;
					worksheet.Cell(currentRow, 4).Value = vehicle.Vehicle_Type;
					worksheet.Cell(currentRow, 5).Value = vehicle.Capacity_Kg;
					worksheet.Cell(currentRow, 6).Value = vehicle.Length_M;
					worksheet.Cell(currentRow, 7).Value = vehicle.Width_M;
					worksheet.Cell(currentRow, 8).Value = vehicle.Height_M;
					worksheet.Cell(currentRow, 9).Value = vehicle.CollectionUnit; 

					string statusText = (vehicle.Status == VehicleStatus.DANG_HOAT_DONG.ToString())
										? "Còn hoạt động"
										: "Không hoạt động";
					worksheet.Cell(currentRow, 10).Value = statusText;
					currentRow++;
				}

				// 3. Thêm bảng chú thích ở cột L (Cột 12) để tránh ghi đè
				var legendHeader = worksheet.Cell(1, 12);
				legendHeader.Value = "Trạng thái hợp lệ";
				legendHeader.Style.Font.Bold = true;
				legendHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6");
				legendHeader.Style.Font.FontColor = XLColor.White;
				legendHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

				worksheet.Cell(2, 12).Value = "Còn hoạt động";
				worksheet.Cell(3, 12).Value = "Không hoạt động";

				var legendRange = worksheet.Range("L1:L3");
				legendRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				legendRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
				legendRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				worksheet.Column(12).Width = 20;

				// 4. Căn chỉnh và kẻ viền cho TOÀN BỘ 10 cột dữ liệu
				worksheet.Columns(1, 10).AdjustToContents();
				var dataRange = worksheet.Range(1, 1, Math.Max(currentRow - 1, 1), 10);
				dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

				using (var stream = new MemoryStream())
				{
					workbook.SaveAs(stream);
					return stream.ToArray();
				}
			}
		}
	}
}

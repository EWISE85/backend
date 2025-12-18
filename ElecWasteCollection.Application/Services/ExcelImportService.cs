using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ExcelImportService : IExcelImportService
	{
		private readonly ICollectionCompanyService _collectionCompanyService;
		private readonly IAccountService _accountService;
		private readonly IUserService _userService;
		private readonly ISmallCollectionService _smallCollectionPointService; // New service
		private readonly ICollectorService _collectorService; // New service
		private readonly IShiftService _shiftService;
		private readonly IVehicleService _vehicleService;


		public ExcelImportService(ICollectionCompanyService collectionCompanyService, IAccountService accountService, IUserService userService, ISmallCollectionService smallCollectionPointService, ICollectorService collectorService, IShiftService shiftService, IVehicleService vehicleService)
		{
			_collectionCompanyService = collectionCompanyService;
			_accountService = accountService;
			_userService = userService;
			_smallCollectionPointService = smallCollectionPointService;
			_collectorService = collectorService;
			_shiftService = shiftService;
			_vehicleService = vehicleService;
		}

		public async Task<ImportResult> ImportAsync(Stream excelStream, string importType)
		{
			var result = new ImportResult();
			try
			{
				using var workbook = new XLWorkbook(excelStream);
				var worksheet = workbook.Worksheet(1);

				if (importType.Equals("Company", StringComparison.OrdinalIgnoreCase))
				{
					await ImportCompanyAsync(worksheet, result);
				}
				else if (importType.Equals("SmallCollectionPoint", StringComparison.OrdinalIgnoreCase))
				{
					await ImportSmallCollectionPointAsync(worksheet, result); // New import for SmallCollectionPoint
				}
				else if (importType.Equals("Collector", StringComparison.OrdinalIgnoreCase))
				{
					await ImportCollectorAsync(worksheet, result); // New import for collector
				}
				else if (importType.Equals("Shift", StringComparison.OrdinalIgnoreCase))
				{
					await ImportShiftAsync(worksheet, result); // New import for collector
				}
				else if (importType.Equals("Vehicle", StringComparison.OrdinalIgnoreCase))
				{
					await ImportVehicleAsync(worksheet, result); // New import for collector
				}
				else if (importType.Equals("User", StringComparison.OrdinalIgnoreCase))
				{
					await ImportUserAsync(worksheet, result);
				}
				else
				{
					result.Success = false;
					result.Messages.Add($"Import type '{importType}' chưa được hỗ trợ.");
					return result;
				}

				result.Success = true;
			}
			catch (Exception ex)
			{
				result.Success = false;
				result.Messages.Add(ex.Message);
			}
			return result;
		}

		private async Task ImportVehicleAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				var plateNumber = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var vehicleType = worksheet.Cell(row, 4).Value.ToString()?.Trim();
				var capacityKgStr = worksheet.Cell(row, 5).Value.ToString();
				int.TryParse(capacityKgStr, out int capacityKg);
				var capacityM3Str = worksheet.Cell(row, 6).Value.ToString();
				int.TryParse(capacityM3Str, out int capacityM3);
				var smallCollectionPointId = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 8).Value.ToString();

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(plateNumber) || string.IsNullOrEmpty(smallCollectionPointId))
				{
					result.Messages.Add($"Dữ liệu thiếu ở dòng {row}.");
					continue;
				}

				// 3. XỬ LÝ TRẠNG THÁI (Status Logic)
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				if (statusNormalized == "còn hoạt động" || statusNormalized == "active")
				{
					statusToSave = VehicleStatus.Active.ToString(); // Hoặc Enum
				}
				else
				{
					statusToSave = VehicleStatus.Inactive.ToString();
				}

				var vehicleModel = new CreateVehicleModel
				{
					VehicleId = id,
					Plate_Number = plateNumber,
					Vehicle_Type = vehicleType,
					Capacity_Kg = capacityKg,
					Capacity_M3 = capacityM3,
					Small_Collection_Point = smallCollectionPointId,
					Status = statusToSave
				};

				var importResult = await _vehicleService.CheckAndUpdateVehicleAsync(vehicleModel);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		// chưa sửa lại id của collector
		private async Task ImportShiftAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				var collectorId = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var dateString = worksheet.Cell(row, 5).Value.ToString()?.Trim();
				var startTimeString = worksheet.Cell(row, 6).Value.ToString()?.Trim();
				var endTimeString = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var smallCollectionPointId = worksheet.Cell(row, 8).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 9).Value.ToString();

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(collectorId) || string.IsNullOrEmpty(dateString))
				{
					result.Messages.Add($"Dữ liệu thiếu ở dòng {row}.");
					continue;
				}

				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave = (statusNormalized == "còn hoạt động" || statusNormalized == "active") ? "Active" : "Inactive";

				// Parse Ngày
				string[] formats = { "dd-MM-yyyy", "d-M-yyyy", "dd/MM/yyyy", "d/M/yyyy" };
				if (!DateOnly.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly workDate))
				{
					result.Messages.Add($"Ngày làm lỗi định dạng dòng {row}: {dateString}");
					continue;
				}

				DateTime shiftStartDateTime;
				DateTime shiftEndDateTime;
				try
				{
					var timeStart = TimeOnly.Parse(startTimeString); 
					var timeEnd = TimeOnly.Parse(endTimeString);

					var tempStart = workDate.ToDateTime(timeStart);
					var tempEnd = workDate.ToDateTime(timeEnd);


					var tempStartUtc = tempStart.AddHours(-7);
					var tempEndUtc = tempEnd.AddHours(-7);

					shiftStartDateTime = DateTime.SpecifyKind(tempStartUtc, DateTimeKind.Utc);
					shiftEndDateTime = DateTime.SpecifyKind(tempEndUtc, DateTimeKind.Utc);
				}
				catch
				{
					result.Messages.Add($"Giờ làm lỗi định dạng dòng {row}.");
					continue;
				}

				var shiftModel = new CreateShiftModel
				{
					ShiftId = id,
					CollectorId = Guid.Parse(collectorId),
					WorkDate = workDate,
					Shift_Start_Time = shiftStartDateTime,
					Shift_End_Time = shiftEndDateTime,
					Status = statusToSave
				};

				var importResult = await _shiftService.CheckAndUpdateShiftAsync(shiftModel);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		// chưa sửa lại id của collector
		private async Task ImportCollectorAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++) // Bỏ qua dòng tiêu đề
			{
				var id = worksheet.Cell(row, 2).Value.ToString()?.Trim(); 
				var name = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var email = worksheet.Cell(row, 4).Value.ToString()?.Trim(); 
				var phone = worksheet.Cell(row, 5).Value.ToString()?.Trim(); 
				var avatar = worksheet.Cell(row, 6).Value.ToString()?.Trim(); 
				var smallCollectionPointId = worksheet.Cell(row, 7).Value.ToString()?.Trim(); 
				var companyId = worksheet.Cell(row, 8).Value.ToString()?.Trim(); 
				var rawStatus = worksheet.Cell(row, 9).Value.ToString(); 
				var collectorUsername = worksheet.Cell(row, 10).Value.ToString()?.Trim(); 
				var collectorPassword = worksheet.Cell(row, 11).Value.ToString()?.Trim();

				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				// Map "Đang làm việc" -> Active
				if (statusNormalized == "đang làm việc")
				{
					statusToSave = UserStatus.Active.ToString(); // Hoặc UserStatus.Active.ToString()
				}
				else if (statusNormalized == "nghỉ việc" || statusNormalized == "ngưng hoạt động")
				{
					statusToSave = UserStatus.Inactive.ToString();
				}
				else
				{
					statusToSave = UserStatus.Inactive.ToString();
				}

				// 3. VALIDATE
				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(smallCollectionPointId) || smallCollectionPointId == "0")
				{
					result.Messages.Add($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.");
					continue;
				}

				// 4. TẠO OBJECT
				// Lưu ý: ID trong Excel là "FPT-C-01" (String), không thể Parse sang Guid được.
				// Nếu UserId trong DB là String:
				var collector = new User
				{
					UserId = Guid.Parse(id), // Sửa: Bỏ Guid.Parse vì ID excel là string "FPT-C-01"
					Name = name,
					Email = email,
					Phone = phone,
					Avatar = avatar,
					SmallCollectionPointId = smallCollectionPointId,
					CollectionCompanyId = companyId,
					Role = UserRole.Collector.ToString(),
					Status = statusToSave, // Dùng status đã xử lý
				};
				var importResult = await _collectorService.CheckAndUpdateCollectorAsync(collector, collectorUsername, collectorPassword);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		private async Task ImportSmallCollectionPointAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				var name = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var address = worksheet.Cell(row, 4).Value.ToString()?.Trim();
				var latitudeVal = worksheet.Cell(row, 5).Value.ToString();
				var latitude = double.TryParse(latitudeVal, out var lat) ? lat : 0;
				var longitudeVal = worksheet.Cell(row, 6).Value.ToString();
				var longitude = double.TryParse(longitudeVal, out var lon) ? lon : 0;
				var openTime = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var companyId = worksheet.Cell(row, 8).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 9).Value.ToString();
				var adminUsername = worksheet.Cell(row, 10).Value.ToString()?.Trim();
				var adminPassword = worksheet.Cell(row, 11).Value.ToString()?.Trim();
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				if (statusNormalized == "còn hoạt động")
				{
					statusToSave = SmallCollectionPointStatus.Active.ToString(); 
				}
				else if (statusNormalized == "ngưng hoạt động")
				{
					statusToSave = SmallCollectionPointStatus.Inactive.ToString();
				}
				else
				{
					statusToSave = SmallCollectionPointStatus.Inactive.ToString();

				}

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(companyId) || companyId == "0")
				{
					result.Messages.Add($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.");
					continue;
				}

				var smallCollectionPoint = new SmallCollectionPoints
				{
					SmallCollectionPointsId = id,
					Name = name,
					Address = address,
					Latitude = latitude,
					Longitude = longitude,
					Status = statusToSave, 
					CompanyId = companyId,
					OpenTime = openTime,
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};
				var importResult = await _smallCollectionPointService.CheckAndUpdateSmallCollectionPointAsync(smallCollectionPoint, adminUsername, adminPassword);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		private async Task ImportCompanyAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();

			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				var name = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var companyEmail = worksheet.Cell(row, 4).Value.ToString()?.Trim();
				var phone = worksheet.Cell(row, 5).Value.ToString()?.Trim();
				var address = worksheet.Cell(row, 6).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var adminUsername = worksheet.Cell(row, 8).Value.ToString()?.Trim();
				var adminPassword = worksheet.Cell(row, 9).Value.ToString()?.Trim();
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();

				string statusToSave;

				if (statusNormalized == "còn hoạt động")
				{
					statusToSave = CompanyStatus.Active.ToString();
				}
				else if (statusNormalized == "ngưng hoạt động")
				{
					statusToSave = CompanyStatus.Inactive.ToString();
				}
				else
				{
					statusToSave = CompanyStatus.Inactive.ToString();
				}

				var company = new CollectionCompany
				{
					CollectionCompanyId = id,
					Name = name,
					CompanyEmail = companyEmail,
					Phone = phone,
					Address = address,
					Status = statusToSave, 
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};

				// Gọi phương thức CheckAndUpdateCompanyAsync
				var importResult = await _collectionCompanyService.CheckAndUpdateCompanyAsync(company, adminUsername, adminPassword);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		private Task ImportUserAsync(IXLWorksheet worksheet, ImportResult result)
		{
			result.Messages.Add("Chức năng import user chưa được implement.");
			return Task.CompletedTask;
		}
	}
}

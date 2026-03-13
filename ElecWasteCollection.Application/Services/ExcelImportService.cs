using ClosedXML.Excel;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ExcelImportService : IExcelImportService
	{
		private readonly ICompanyService _companyService;
		private readonly IAccountService _accountService;
		private readonly IUserService _userService;
		private readonly ISmallCollectionService _smallCollectionPointService;
		private readonly ICollectorService _collectorService;
		private readonly IShiftService _shiftService;
		private readonly IVehicleService _vehicleService;
		private readonly IEmailService _emailService;
		private readonly IMapboxService _mapboxService;


		public ExcelImportService(ICompanyService CompanyService, IAccountService accountService, IUserService userService, ISmallCollectionService smallCollectionPointService, ICollectorService collectorService, IShiftService shiftService, IVehicleService vehicleService, IEmailService emailService, IMapboxService mapboxService)
		{
			_companyService = CompanyService;
			_accountService = accountService;
			_userService = userService;
			_smallCollectionPointService = smallCollectionPointService;
			_collectorService = collectorService;
			_shiftService = shiftService;
			_vehicleService = vehicleService;
			_emailService = emailService;
			_mapboxService = mapboxService;
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
					await ImportSmallCollectionPointAsync(worksheet, result); 
				}
				else if (importType.Equals("Collector", StringComparison.OrdinalIgnoreCase))
				{
					await ImportCollectorAsync(worksheet, result); 
				}
				else if (importType.Equals("Shift", StringComparison.OrdinalIgnoreCase))
				{
					await ImportShiftAsync(worksheet, result); 
				}
				else if (importType.Equals("Vehicle", StringComparison.OrdinalIgnoreCase))
				{
					await ImportVehicleAsync(worksheet, result); 
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
                //var capacityM3Str = worksheet.Cell(row, 6).Value.ToString();
                //int.TryParse(capacityM3Str, out int capacityM3);
                var lengthStr = worksheet.Cell(row, 6).Value.ToString();
                double.TryParse(lengthStr, out double lengthM);
                var widthStr = worksheet.Cell(row, 7).Value.ToString();
                double.TryParse(widthStr, out double widthM);
                var heightStr = worksheet.Cell(row, 8).Value.ToString();
                double.TryParse(heightStr, out double heightM);
                var smallCollectionPointId = worksheet.Cell(row, 9).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 10).Value.ToString();

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
					statusToSave = VehicleStatus.DANG_HOAT_DONG.ToString(); // Hoặc Enum
				}
				else
				{
					statusToSave = VehicleStatus.KHONG_HOAT_DONG.ToString();
				}

				var vehicleModel = new CreateVehicleModel
				{
					VehicleId = id,
					Plate_Number = plateNumber,
					Vehicle_Type = vehicleType,
					Capacity_Kg = capacityKg,
                    Length_M = lengthM,
                    Width_M = widthM,
                    Height_M = heightM,
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
				

				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				// Map "Đang làm việc" -> Active
				if (statusNormalized == "đang làm việc")
				{
					statusToSave = UserStatus.DANG_HOAT_DONG.ToString(); // Hoặc UserStatus.Active.ToString()
				}
				else if (statusNormalized == "nghỉ việc" || statusNormalized == "ngưng hoạt động")
				{
					statusToSave = UserStatus.KHONG_HOAT_DONG.ToString();
				}
				else
				{
					statusToSave = UserStatus.KHONG_HOAT_DONG.ToString();
				}

				// 3. VALIDATE
				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(smallCollectionPointId) || smallCollectionPointId == "0")
				{
					result.Messages.Add($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.");
					continue;
				}

				var defaultSettings = new UserSettingsModel
				{
					ShowMap = false 
				};
				var collectorUsername = string.IsNullOrEmpty(email) ? $"collector_{id}" : email;
				var collectorPassword = GenerateRandomPassword(6);
				var collector = new User
				{
					UserId = Guid.Parse(id), 
					Name = name,
					Email = email,
					Phone = phone,
					Avatar = avatar,
					SmallCollectionPointId = smallCollectionPointId,
					CollectionCompanyId = companyId,
					Role = UserRole.Collector.ToString(),
					Status = statusToSave, 
				};
				var importResult = await _collectorService.CheckAndUpdateCollectorAsync(collector, collectorUsername, collectorPassword);
				result.Messages.AddRange(importResult.Messages);
				if (importResult.IsNew)
				{
					string emailSubject = "Thông tin tài khoản quản trị hệ thống";
					string emailBody = $@"Kính gửi {name},

Hệ thống đã tạo thành công tài khoản quản trị cho bạn. Dưới đây là thông tin đăng nhập:

- Tên đăng nhập: {collectorUsername}
- Mật khẩu: {collectorPassword}

Vui lòng đăng nhập vào hệ thống và đổi lại mật khẩu trong lần đầu tiên sử dụng để đảm bảo tính bảo mật.

Trân trọng,
Ban Quản Trị Hệ Thống";
					try
					{
						await _emailService.SendEmailAsync(email, emailSubject, emailBody);
						result.Messages.Add($"Đã gửi email cấp tài khoản cho {email}");
					}
					catch (Exception ex)
					{
						result.Messages.Add($"Lỗi gửi email cho {email}: {ex.Message}");
					}
				}
			}
		}

		private async Task ImportSmallCollectionPointAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString().Trim();
				var name = worksheet.Cell(row, 3).Value.ToString().Trim();
				var address = worksheet.Cell(row, 4).Value.ToString().Trim();
				var email = worksheet.Cell(row, 5).Value.ToString().Trim();
				var phone = worksheet.Cell(row, 6).Value.ToString().Trim();
				var openTime = worksheet.Cell(row, 7).Value.ToString().Trim();
				var maxCapacity = worksheet.Cell(row, 8).Value.ToString().Trim();
				var companyId = worksheet.Cell(row, 9).Value.ToString().Trim();
				var rawStatus = worksheet.Cell(row, 10).Value.ToString();
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				if (statusNormalized.Equals("còn hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = SmallCollectionPointStatus.DANG_HOAT_DONG.ToString(); 
				}
				else if (statusNormalized.Equals("không hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = SmallCollectionPointStatus.KHONG_HOAT_DONG.ToString();
				}
				else
				{
					statusToSave = SmallCollectionPointStatus.KHONG_HOAT_DONG.ToString();

				}

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(companyId) || companyId == "0")
				{
					result.Messages.Add($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.");
					continue;
				}
				var adminUsername = string.IsNullOrEmpty(email) ? $"admin_{id}" : email;
				var adminPassword = GenerateRandomPassword(6);
				double latitude = 0;
				double longitude = 0;

				if (!string.IsNullOrEmpty(address))
				{
					var coordinates = await _mapboxService.GetCoordinatesFromAddressAsync(address);
					if (coordinates.HasValue)
					{
						latitude = coordinates.Value.Latitude;
						longitude = coordinates.Value.Longitude;
					}
					else
					{
						result.Messages.Add($"Cảnh báo dòng {row}: Không thể tìm thấy tọa độ cho địa chỉ '{address}'. Đã đặt tọa độ về 0.");
					}
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
					MaxCapacity = Double.Parse(maxCapacity),
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};
				var importResult = await _smallCollectionPointService.CheckAndUpdateSmallCollectionPointAsync(smallCollectionPoint, adminUsername, adminPassword);
				result.Messages.AddRange(importResult.Messages);
				if (importResult.IsNew)
				{
					string emailSubject = "Thông tin tài khoản quản trị hệ thống";
					string emailBody = $@"Kính gửi {name},

Hệ thống đã tạo thành công tài khoản quản trị cho kho của bạn. Dưới đây là thông tin đăng nhập:

- Tên đăng nhập: {adminUsername}
- Mật khẩu: {adminPassword}

Vui lòng đăng nhập vào hệ thống và đổi lại mật khẩu trong lần đầu tiên sử dụng để đảm bảo tính bảo mật.

Trân trọng,
Ban Quản Trị Hệ Thống";
					try
					{
						await _emailService.SendEmailAsync(email, emailSubject, emailBody);
						result.Messages.Add($"Đã gửi email cấp tài khoản cho {email}");
					}
					catch (Exception ex)
					{
						result.Messages.Add($"Lỗi gửi email cho {email}: {ex.Message}");
					}
				}
			}
		}

		private async Task ImportCompanyAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();

			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 2).Value.ToString().Trim();
				var name = worksheet.Cell(row, 3).Value.ToString().Trim();
				var companyEmail = worksheet.Cell(row, 4).Value.ToString().Trim();
				var phone = worksheet.Cell(row, 5).Value.ToString().Trim();
				var address = worksheet.Cell(row, 6).Value.ToString().Trim();
				var companyType = worksheet.Cell(row, 7).Value.ToString().Trim();
				var rawStatus = worksheet.Cell(row, 8).Value.ToString().Trim();
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();

				var adminUsername = string.IsNullOrEmpty(companyEmail) ? $"admin_{id}" : companyEmail;
				var adminPassword = GenerateRandomPassword(6);
				string statusToSave;
				string companyTypeToSave;

				if (statusNormalized.Equals("Còn hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = CompanyStatus.DANG_HOAT_DONG.ToString();
				}
				else if (statusNormalized.Equals("Ngưng hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = CompanyStatus.KHONG_HOAT_DONG.ToString();
				}
				else
				{
					statusToSave = CompanyStatus.KHONG_HOAT_DONG.ToString();
				}

				if (companyType.Equals("Collection Company", StringComparison.OrdinalIgnoreCase) || companyType.Equals("Công ty thu gom", StringComparison.OrdinalIgnoreCase))
				{
					companyTypeToSave = CompanyType.CTY_THU_GOM.ToString();
				}
				else if (companyType.Equals("Recycling Company", StringComparison.OrdinalIgnoreCase) || companyType.Equals("Công ty tái chế", StringComparison.OrdinalIgnoreCase))
				{
					companyTypeToSave = CompanyType.CTY_TAI_CHE.ToString();
				}
				else
				{
					companyTypeToSave = CompanyType.CTY_THU_GOM.ToString();
				}
				var company = new Company
				{
					CompanyId = id,
					Name = name,
					CompanyEmail = companyEmail,
					Phone = phone,
					Address = address,
					CompanyType = companyTypeToSave,
					Status = statusToSave, 
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};

				// Gọi phương thức CheckAndUpdateCompanyAsync
				var importResult = await _companyService.CheckAndUpdateCompanyAsync(company, adminUsername, adminPassword);
				result.Messages.AddRange(importResult.Messages);
				if (importResult.IsNew)
				{
					string emailSubject = "Thông tin tài khoản quản trị hệ thống";
					string emailBody = $@"Kính gửi {name},

Hệ thống đã tạo thành công tài khoản quản trị cho công ty của bạn. Dưới đây là thông tin đăng nhập:

- Tên đăng nhập: {adminUsername}
- Mật khẩu: {adminPassword}

Vui lòng đăng nhập vào hệ thống và đổi lại mật khẩu trong lần đầu tiên sử dụng để đảm bảo tính bảo mật.

Trân trọng,
Ban Quản Trị Hệ Thống";
					try
					{
						await _emailService.SendEmailAsync(companyEmail, emailSubject, emailBody);
						result.Messages.Add($"Đã gửi email cấp tài khoản cho {companyEmail}");
					}
					catch (Exception ex)
					{
						result.Messages.Add($"Lỗi gửi email cho {companyEmail}: {ex.Message}");
					}
				}
				
			}
		}

		private Task ImportUserAsync(IXLWorksheet worksheet, ImportResult result)
		{
			result.Messages.Add("Chức năng import user chưa được implement.");
			return Task.CompletedTask;
		}

		private string GenerateRandomPassword(int length = 10)
		{
			// Đảm bảo mật khẩu có đủ chữ hoa, chữ thường, số và ký tự đặc biệt
			const string validChars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*?_-";
			var random = new Random();
			return new string(Enumerable.Repeat(validChars, length)
				.Select(s => s[random.Next(s.Length)]).ToArray());
		}
	}
}

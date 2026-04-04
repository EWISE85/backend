using ClosedXML.Excel;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
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
		private readonly ICollectionUnitService _smallCollectionPointService;
		private readonly ICollectorService _collectorService;
		private readonly IShiftService _shiftService;
		private readonly IVehicleService _vehicleService;
		private readonly IEmailService _emailService;
		private readonly IMapboxService _mapboxService;
		private readonly IVoucherService _voucherService;
		private	readonly IUserRepository _userRepository;
		private readonly IPublicHolidayService _publicHolidayService;
		private readonly IAttributeService _attributeService;
		private readonly IBrandService _brandService;
		private readonly ICategoryService _categoryService;
		private readonly IBrandCategoryService _brandCategoryService;
		private readonly ICategoryAttributeService _categoryAttributeService;


		public ExcelImportService(ICompanyService CompanyService, IAccountService accountService, IUserService userService, ICollectionUnitService smallCollectionPointService, ICollectorService collectorService, IShiftService shiftService, IVehicleService vehicleService, IEmailService emailService, IMapboxService mapboxService, IVoucherService voucherService, IUserRepository userRepository, IPublicHolidayService publicHolidayService, IAttributeService attributeService, IBrandService brandService, ICategoryService categoryService, IBrandCategoryService brandCategoryService, ICategoryAttributeService categoryAttributeService)
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
			_voucherService = voucherService;
			_userRepository = userRepository;
			_publicHolidayService = publicHolidayService;
			_attributeService = attributeService;
			_brandService = brandService;
			_categoryService = categoryService;
			_brandCategoryService = brandCategoryService;
			_categoryAttributeService = categoryAttributeService;
		}

		public async Task<ImportResult> ImportAsync(Stream excelStream, string importType)
		{
			var result = new ImportResult();
			try
			{
				using var workbook = new XLWorkbook(excelStream);

				if (importType.Equals("CategorySystem", StringComparison.OrdinalIgnoreCase))
				{
					//Thứ tự cực kỳ quan trọng để tránh lỗi khóa ngoại
					await ImportCategoriesAsync(workbook.Worksheet(1), result);
					await ImportBrandsAsync(workbook.Worksheet(2), result);
					await ImportAttributesAsync(workbook.Worksheet(4), result);

					//// Sau khi có dữ liệu gốc mới tiến hành Map
					await ImportCategoryBrandMapAsync(workbook.Worksheet(3), result);
					await ImportCategoryAttributeMapAsync(workbook.Worksheet(5), result);
				}
				else
				{
					var worksheet = workbook.Worksheet(1);

					if (importType.Equals("Company", StringComparison.OrdinalIgnoreCase))
					{
						await ImportCompanyAsync(worksheet, result);
					}
					else if (importType.Equals("CollectionUnit", StringComparison.OrdinalIgnoreCase))
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
					else if (importType.Equals("Voucher", StringComparison.OrdinalIgnoreCase))
					{
						await ImportVoucherAsync(worksheet, result);
					}
					else if (importType.Equals("PublicHoliday", StringComparison.OrdinalIgnoreCase))
					{
						await ImportPublicHolidayAsync(worksheet, result);
					}
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

		private async Task ImportPublicHolidayAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			var importedNames = new List<string>(); // Danh sách lưu tên từ Excel

			for (int row = 2; row <= rowCount; row++)
			{
				var name = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				if (string.IsNullOrWhiteSpace(name)) continue;

				importedNames.Add(name); // Thêm tên vào danh sách

				var description = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var startAtStr = worksheet.Cell(row, 4).Value.ToString()?.Trim();
				var endAtStr = worksheet.Cell(row, 5).Value.ToString()?.Trim();
				string dateFormat = "dd/MM/yyyy";

				DateOnly startAt = DateOnly.TryParseExact(startAtStr, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempStart)
					? tempStart
					: DateOnly.MinValue;

				DateOnly endAt = DateOnly.TryParseExact(endAtStr, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempEnd)
					? tempEnd
					: DateOnly.MinValue;

				var publicHoliday = new PublicHoliday
				{
					Name = name,
					Description = description,
					StartDate = startAt,
					EndDate = endAt,
					IsActive = true,
					CreatedAt = DateTime.UtcNow
				};

				var importResult = await _publicHolidayService.CheckAndUpdatePublicHolidayAsync(publicHoliday);
				result.Messages.AddRange(importResult.Messages);
			}

			await _publicHolidayService.DeactivateMissingHolidaysAsync(importedNames, result);
		}

		private async Task ImportVoucherAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var code = worksheet.Cell(row, 2).Value.ToString()?.Trim();
				var name = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var imageUrl = worksheet.Cell(row, 4).Value.ToString()?.Trim();
				var description = worksheet.Cell(row, 5).Value.ToString()?.Trim();
				var startAtStr = worksheet.Cell(row, 6).Value.ToString()?.Trim();
				var endAtStr = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var valueStr = worksheet.Cell(row, 8).Value.ToString()?.Trim();
				var pointsToRedeemStr = worksheet.Cell(row, 9).Value.ToString()?.Trim();
				var rawStatus = worksheet.Cell(row, 10).Value.ToString();
				string dateFormat = "dd/MM/yyyy";

				DateOnly startAt = DateOnly.TryParseExact(startAtStr?.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempStart)
					? tempStart
					: DateOnly.MinValue;

				DateOnly endAt = DateOnly.TryParseExact(endAtStr?.Trim(), dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempEnd)
					? tempEnd
					: DateOnly.MinValue;
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				if (statusNormalized.Equals("hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = VoucherStatus.HOAT_DONG.ToString(); // Hoặc Enum
				}
				else
				{
					statusToSave = VoucherStatus.KHONG_HOAT_DONG.ToString();
				}
				var voucherModel = new CreateVoucherModel
				{
					Code = code,
					Name = name,
					ImageUrl = imageUrl,
					Description = description,
					StartAt = startAt,
					EndAt = endAt,
					Value = double.TryParse(valueStr, out var tempValue) ? tempValue : 0,
					PointsToRedeem = double.TryParse(pointsToRedeemStr, out var tempPoints) ? tempPoints : 0,
					Status = statusToSave
				};
				var importResult = await _voucherService.CheckAndUpdateVoucherAsync(voucherModel);
				result.Messages.AddRange(importResult.Messages);
			};
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
					throw new AppException($"Dữ liệu thiếu ở dòng {row}.", 400);
				}

				// 3. XỬ LÝ TRẠNG THÁI (Status Logic)
				var statusNormalized = string.IsNullOrEmpty(rawStatus) ? "" : rawStatus.Trim().ToLower();
				string statusToSave;

				if (statusNormalized.Equals("còn hoạt động", StringComparison.OrdinalIgnoreCase) || statusNormalized == "active")
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
				var collectorCode = worksheet.Cell(row, 3).Value.ToString()?.Trim();
				var dateString = worksheet.Cell(row, 5).Value.ToString()?.Trim();
				var startTimeString = worksheet.Cell(row, 6).Value.ToString()?.Trim();
				var endTimeString = worksheet.Cell(row, 7).Value.ToString()?.Trim();
				var smallCollectionPointId = worksheet.Cell(row, 8).Value.ToString()?.Trim();

				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(collectorCode) || string.IsNullOrEmpty(dateString))
				{
					result.Messages.Add($"Dữ liệu thiếu ở dòng {row}.");
					throw new AppException($"Dữ liệu thiếu ở dòng {row}.", 400);
				}



				// Parse Ngày
				DateOnly workDate;
				var dateCell = worksheet.Cell(row, 5);
				if (dateCell.TryGetValue(out DateTime rawDate))
				{
					workDate = DateOnly.FromDateTime(rawDate);
				}
				else
				{
					result.Messages.Add($"Ngày làm tại dòng {row} không đúng định dạng Excel Date.");
					throw new AppException($"Ngày làm tại dòng {row} không đúng định dạng Excel Date.", 400);
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
					throw new AppException($"Giờ làm lỗi định dạng dòng {row}.", 400);
				}

				var collector = await _userRepository.GetAsync(c => c.CollectorCode == collectorCode);
				if (collector == null)
				{
					result.Messages.Add($"Không tìm thấy collector với mã '{collectorCode}' ở dòng {row}.");
					throw new AppException($"Không tìm thấy collector với mã '{collectorCode}' ở dòng {row}.", 400);
				}
				var shiftModel = new CreateShiftModel
				{
					ShiftId = id,
					CollectorId = collector.UserId,
					WorkDate = workDate,
					Shift_Start_Time = shiftStartDateTime,
					Shift_End_Time = shiftEndDateTime,
					Status = ShiftStatus.CO_SAN.ToString(),
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
				var code = worksheet.Cell(row, 2).Value.ToString()?.Trim(); 
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
					throw new AppException($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.", 400);
				}

				var defaultSettings = new UserSettingsModel
				{
					ShowMap = false 
				};
				var collectorUsername = string.IsNullOrEmpty(email) ? $"collector_{code}" : email;
				var collectorPassword = GenerateRandomPassword(6);
				var collector = new User
				{
					Name = name,
					Email = email,
					Phone = phone,
					Avatar = avatar,
					CollectorCode = code,
                    CollectionUnitId = smallCollectionPointId,
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
					statusToSave = CollectionUnitStatus.DANG_HOAT_DONG.ToString(); 
				}
				else if (statusNormalized.Equals("không hoạt động", StringComparison.OrdinalIgnoreCase))
				{
					statusToSave = CollectionUnitStatus.KHONG_HOAT_DONG.ToString();
				}
				else
				{
					statusToSave = CollectionUnitStatus.KHONG_HOAT_DONG.ToString();

				}

				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address) || string.IsNullOrEmpty(companyId) || companyId == "0")
				{
					result.Messages.Add($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.");
					throw new AppException($"Dữ liệu thiếu hoặc không hợp lệ ở dòng {row}.", 400);
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
				var smallCollectionPoint = new CollectionUnit
                {
                    CollectionUnitId = id,
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
					companyTypeToSave = CompanyType.CTY_TAI_CHE.ToString();
				}
				else if (companyType.Equals("Recycling Company", StringComparison.OrdinalIgnoreCase) || companyType.Equals("Công ty tái chế", StringComparison.OrdinalIgnoreCase))
				{
					companyTypeToSave = CompanyType.CTY_TAI_CHE.ToString();
				}
				else
				{
					companyTypeToSave = CompanyType.CTY_TAI_CHE.ToString();
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
		private async Task ImportAttributesAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();

			var excelData = new Dictionary<string, List<AttributeOptions>>();
			string lastAttrName = "";

			for (int row = 2; row <= rowCount; row++)
			{
				var attrName = worksheet.Cell(row, 2).Value.ToString().Trim();
				if (string.IsNullOrEmpty(attrName))
				{
					attrName = lastAttrName;
				}
				else
				{
					lastAttrName = attrName;
				}

				if (string.IsNullOrEmpty(attrName)) continue;

				if (!excelData.ContainsKey(attrName))
				{
					excelData[attrName] = new List<AttributeOptions>();
				}

				var optionName = worksheet.Cell(row, 4).Value.ToString().Trim();

				if (!string.IsNullOrEmpty(optionName))
				{
					var weightStr = worksheet.Cell(row, 5).Value.ToString().Trim();
					var volumeStr = worksheet.Cell(row, 6).Value.ToString().Trim();

					excelData[attrName].Add(new AttributeOptions
					{
						OptionName = optionName,
						EstimateWeight = double.TryParse(weightStr, out var w) ? w : 0,
						EstimateVolume = double.TryParse(volumeStr, out var v) ? v : 0
					});
				}
			}

			foreach (var entry in excelData)
			{
				try
				{
					var attrName = entry.Key;
					var options = entry.Value;

					// Gọi hàm Sync mới để tự động: 
					// 1. Tạo Attribute nếu chưa có.
					// 2. Update option cũ, Add option mới.
					// 3. Tắt (Inactive) các option không có trong file Excel này.
					await _attributeService.SyncAttributeOptionsAsync(attrName, options);

					result.Messages.Add($"Đồng bộ thành công thuộc tính: {attrName} ({options.Count} tùy chọn).");
				}
				catch (Exception ex)
				{
					result.Messages.Add($"Lỗi khi xử lý thuộc tính '{entry.Key}': {ex.Message}");
					result.Success = false;
				}
			}
		}

		private async Task ImportBrandsAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			var excelBrandNames = new List<string>();

			for (int row = 2; row <= rowCount; row++)
			{
				var brandName = worksheet.Cell(row, 2).Value.ToString().Trim();
				if (!string.IsNullOrEmpty(brandName))
				{
					excelBrandNames.Add(brandName);
				}
			}

			try
			{
				await _brandService.SyncBrandsAsync(excelBrandNames);
				result.Messages.Add($"Đã đồng bộ {excelBrandNames.Count} thương hiệu.");
			}
			catch (Exception ex)
			{
				result.Messages.Add($"Lỗi đồng bộ thương hiệu: {ex.Message}");
				result.Success = false;
			}
		}
		private async Task ImportCategoriesAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			var excelCategories = new List<CategoryImportModel>();

			for (int row = 2; row <= rowCount; row++)
			{
				var name = worksheet.Cell(row, 2).Value.ToString().Trim();
				if (string.IsNullOrEmpty(name)) throw new AppException("Tên danh mục đang bị trống",400) ;

				excelCategories.Add(new CategoryImportModel
				{
					Name = name,
					ParentName = worksheet.Cell(row, 3).Value.ToString().Trim(),
					EmissionFactor = double.TryParse(worksheet.Cell(row, 4).Value.ToString(), out var ef) ? ef : 0
				});
			}

			try
			{
				await _categoryService.SyncCategoriesAsync(excelCategories);
				result.Messages.Add($"[Danh mục] Đồng bộ thành công {excelCategories.Count} dòng.");
			}
			catch (Exception ex)
			{
				result.Messages.Add($"[Danh mục] Lỗi hệ thống: {ex.Message}");
				result.Success = false;
			}
		}
		// Import Sheet 3
		private async Task ImportCategoryBrandMapAsync(IXLWorksheet worksheet, ImportResult result)
		{
			var maps = new List<BrandCategoryMapModel>();
			foreach (var row in worksheet.RowsUsed().Skip(1))
			{
				maps.Add(new BrandCategoryMapModel
				{
					CategoryName = row.Cell(2).Value.ToString(),
					BrandName = row.Cell(3).Value.ToString(),
					Points = double.TryParse(row.Cell(4).Value.ToString(), out var p) ? p : 0
				});
			}
			await _brandCategoryService.SyncBrandCategoryMapsAsync(maps);
		}

		// Import Sheet 5
		private async Task ImportCategoryAttributeMapAsync(IXLWorksheet worksheet, ImportResult result)
		{
			var maps = new List<CategoryAttributeMapModel>();
			foreach (var row in worksheet.RowsUsed().Skip(1))
			{
				maps.Add(new CategoryAttributeMapModel
				{
					CategoryName = row.Cell(2).Value.ToString(),
					AttributeName = row.Cell(3).Value.ToString(),
					Unit = row.Cell(4).Value.ToString(),
					MinValue = double.TryParse(row.Cell(5).Value.ToString(), out var min) ? min : null,
					MaxValue = double.TryParse(row.Cell(6).Value.ToString(), out var max) ? max : null
				});
			}
			await _categoryAttributeService.SyncCategoryAttributeMapsAsync(maps);
		}
	}
}

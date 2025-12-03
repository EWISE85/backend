using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
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


		public ExcelImportService(ICollectionCompanyService collectionCompanyService, IAccountService accountService, IUserService userService, ISmallCollectionService smallCollectionPointService, ICollectorService collectorService)
		{
			_collectionCompanyService = collectionCompanyService;
			_accountService = accountService;
			_userService = userService;
			_smallCollectionPointService = smallCollectionPointService;
			_collectorService = collectorService;
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

		private async Task ImportCollectorAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++) // Skip header row
			{
				var id = worksheet.Cell(row, 1).Value.ToString();
				var name = worksheet.Cell(row, 2).Value.ToString();
				var email = worksheet.Cell(row, 3).Value.ToString();
				var phone = worksheet.Cell(row, 4).Value.ToString();
				var avatar = worksheet.Cell(row, 5).Value.ToString();
				var smallCollectionPointId = int.TryParse(worksheet.Cell(row, 6).Value.ToString(), out var collectionId) ? collectionId : 0;
				var companyId = int.TryParse(worksheet.Cell(row, 7).Value.ToString(), out var compId) ? compId : 0;
				var status = worksheet.Cell(row, 7).Value.ToString();
				var collectorUsername = worksheet.Cell(row, 8).Value.ToString();
				var collectorPassword = worksheet.Cell(row, 9).Value.ToString();
				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || smallCollectionPointId == 0)
				{
					result.Messages.Add($"Dữ liệu không hợp lệ ở dòng {row}.");
					continue;
				}
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
					Status = status
				};
				var importResult = await _collectorService.CheckAndUpdateCollectorAsync(collector, collectorUsername, collectorPassword);
				result.Messages.AddRange(importResult.Messages);

			}

		}

		private async Task ImportSmallCollectionPointAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++) // Skip header row
			{
				var id = worksheet.Cell(row, 1).Value.ToString();
				var name = worksheet.Cell(row, 2).Value.ToString();
				var address = worksheet.Cell(row, 3).Value.ToString();
				var latitude = double.TryParse(worksheet.Cell(row, 4).Value.ToString(), out var lat) ? lat : 0;
				var longitude = double.TryParse(worksheet.Cell(row, 5).Value.ToString(), out var lon) ? lon : 0;
				var openTime = worksheet.Cell(row, 6).Value.ToString();
				var companyId = int.TryParse(worksheet.Cell(row, 7).Value.ToString(), out var compId) ? compId : 0;
				var status = worksheet.Cell(row, 8).Value.ToString();
				var adminUsername = worksheet.Cell(row, 9).Value.ToString();
				var adminPassword = worksheet.Cell(row, 10).Value.ToString();

				// Validate required fields
				if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(address) || companyId == 0)
				{
					result.Messages.Add($"Dữ liệu không hợp lệ ở dòng {row}.");
					continue;
				}

				var smallCollectionPoint = new SmallCollectionPoints
				{
					Id = int.Parse(id),
					Name = name,
					Address = address,
					Latitude = latitude,
					Longitude = longitude,
					Status = status,
					CompanyId = companyId,
					OpenTime = openTime,
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};

				// Call service method to check and update the SmallCollectionPoint
				var importResult = await _smallCollectionPointService.CheckAndUpdateSmallCollectionPointAsync(smallCollectionPoint, adminUsername, adminPassword);
				result.Messages.AddRange(importResult.Messages);
			}
		}

		private async Task ImportCompanyAsync(IXLWorksheet worksheet, ImportResult result)
		{
			int rowCount = worksheet.RowsUsed().Count();
			for (int row = 2; row <= rowCount; row++)
			{
				var id = worksheet.Cell(row, 1).Value.ToString();
				var name = worksheet.Cell(row, 2).Value.ToString();
				var companyEmail = worksheet.Cell(row, 3).Value.ToString();
				var phone = worksheet.Cell(row, 4).Value.ToString();
				var address = worksheet.Cell(row, 5).Value.ToString();
				var status = worksheet.Cell(row, 6).Value.ToString();
				var adminUsername = worksheet.Cell(row, 7).Value.ToString();
				var adminPassword = worksheet.Cell(row, 8).Value.ToString();
				var company = new CollectionTeams
				{
					Id = int.Parse(id),
					Name = name,
					CompanyEmail = companyEmail,
					Phone = phone,
					Address = address,
					Status = CompanyStatus.Active.ToString(),
					Created_At = DateTime.UtcNow,
					Updated_At = DateTime.UtcNow
				};

				// Gọi phương thức CheckAndUpdateCompanyAsync thay vì kiểm tra và cập nhật trực tiếp
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

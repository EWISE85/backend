using ClosedXML.Excel;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CollectionUnitService : ICollectionUnitService
    {
		private readonly ICollectionUnitRepository _smallCollectionRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IAccountRepsitory _accountRepository;
		public CollectionUnitService(IUnitOfWork unitOfWork, IUserRepository userRepository, IAccountRepsitory accountRepository, ICollectionUnitRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_accountRepository = accountRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}
		public async Task<bool> AddNewSmallCollectionPoint(CollectionUnit smallCollectionPoints)
		{
			await _unitOfWork.CollectionUnits.AddAsync(smallCollectionPoints);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(CollectionUnit smallCollectionPoints, string adminUsername, string adminPassword, string email, string phone)
		{
			var result = new ImportResult();

			var existingCompany = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPoints.CollectionUnitId);
			if (existingCompany != null)
			{
				await UpdateSmallCollectionPoint(smallCollectionPoints, email, phone);
				result.Messages.Add($"Đã cập nhật thông tin kho '{smallCollectionPoints.Name}'.");
				result.IsNew = false;
			}
			else
			{
				await AddNewSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Thêm kho '{smallCollectionPoints.Name}' thành công.");

                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.CollectionUnitId, SystemConfigKey.RADIUS_KM, "10");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.CollectionUnitId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, "15");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.CollectionUnitId, SystemConfigKey.TRANSPORT_SPEED, "35");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.CollectionUnitId, SystemConfigKey.SERVICE_TIME_MINUTES, "10");
                await UpsertPointConfigAsync(smallCollectionPoints.CompanyId, smallCollectionPoints.CollectionUnitId, SystemConfigKey.WAREHOUSE_LOAD_THRESHOLD, "0.7");
				var role = await _unitOfWork.Roles.GetAsync(r => r.Name == UserRole.AdminWarehouse.ToString());
				if (role == null) throw new AppException("Không tìm thấy vai trò AdminWarehouse", 404);
				var newAdminWarehouse = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = null,
					Email = email,
					Phone = phone,
					Name = "Admin " + smallCollectionPoints.Name,
					RoleId = role.RoleId,
					Status = UserStatus.DANG_HOAT_DONG.ToString(),
					CompanyId = smallCollectionPoints.CompanyId,
                    CollectionUnitId = smallCollectionPoints.CollectionUnitId,
				};
				await _unitOfWork.Users.AddAsync(newAdminWarehouse);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminWarehouse.UserId,
					Username = adminUsername,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
					IsFirstLogin = true
				};
				await _unitOfWork.Accounts.AddAsync(adminAccount);
				result.Messages.Add($"Tạo tài khoản quản trị kho với tên đăng nhập '{adminUsername}'.");
				result.IsNew = true;
				await _unitOfWork.SaveAsync();
			}

			return result;
		}
        private async Task UpsertPointConfigAsync(string companyId, string pointId, SystemConfigKey key, string value)
        {
            var existingConfig = await _unitOfWork.SystemConfig.GetAsync(x =>
                x.Key == key.ToString() &&
                x.CollectionUnitId == pointId);

            if (existingConfig != null)
            {
                existingConfig.Value = value;
                _unitOfWork.SystemConfig.Update(existingConfig);
            }
            else
            {
                string displayName = key switch
                {
                    SystemConfigKey.RADIUS_KM => "Bán kính thu gom",
                    SystemConfigKey.MAX_ROAD_DISTANCE_KM => "Khoảng cách di chuyển tối đa (km)",
                    SystemConfigKey.TRANSPORT_SPEED => "Tốc độ di chuyển (km/h)",
                    SystemConfigKey.SERVICE_TIME_MINUTES => "Thời gian phục vụ tại điểm (phút)",
                    SystemConfigKey.WAREHOUSE_LOAD_THRESHOLD => "Ngưỡng tải trọng kho hàng",
                    _ => key.ToString()
                };

                var newConfig = new SystemConfig
                {
                    SystemConfigId = Guid.NewGuid(),
                    Key = key.ToString(),
                    Value = value,
                    CompanyId = companyId,
                    CollectionUnitId = pointId,
                    Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                    DisplayName = displayName,
                    GroupName = "PointConfig"
                };
                await _unitOfWork.SystemConfig.AddAsync(newConfig);
            }
        }

        public async Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho",404);
			smallPoint.Status = CollectionUnitStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.CollectionUnits.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model)
		{
			var (entities, totalItems) = await _smallCollectionRepository.GetPagedAsync(
				companyId: model.CompanyId,
				status: model.Status,
				page: model.Page,
				limit: model.Limit
			);
			var resultList = entities.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return new PagedResultModel<SmallCollectionPointsResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}

		public async Task<SmallCollectionPointsResponse> GetSmallCollectionById(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			
				return new SmallCollectionPointsResponse
				{
					Id = smallPoint.CollectionUnitId,
					CompanyId = smallPoint.CompanyId,
					Name = smallPoint.Name,
					Address = smallPoint.Address,
					Latitude = smallPoint.Latitude,
					Longitude = smallPoint.Longitude,
					OpenTime = smallPoint.OpenTime,
					Status = smallPoint.Status
				};

		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointActive()
		{
			var smallPoints = await _smallCollectionRepository.GetAllAsync(s => s.Status == CollectionUnitStatus.DANG_HOAT_DONG.ToString(),
				includeProperties: "Company.CompanyRecyclingCategories.Category.SubCategories");
			return smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status,
				CompanyName = point.Company?.Name,
				AcceptedCategories = point.Company?.CompanyRecyclingCategories
					.Where(crc => crc.Category.Status == CategoryStatus.HOAT_DONG.ToString())
					.SelectMany(crc => crc.Category.SubCategories)
					.Where(sub => sub.Status == CategoryStatus.HOAT_DONG.ToString())
					.Select(sub => new CategoryModel
					{
						Id = sub.CategoryId,
						Name = sub.Name
					}).ToList() ?? new List<CategoryModel>()
			}).ToList();
		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointByCompanyId(string companyId)
		{
			var smallPoints = await _smallCollectionRepository.GetsAsync(s => s.CompanyId == companyId);
			var result = smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return result;
		}

		public async Task<bool> UpdateSmallCollectionPoint(CollectionUnit smallCollectionPoints, string email, string phone)
		{
			var smallPoint = await _unitOfWork.CollectionUnits.GetAsync(
				x => x.CollectionUnitId == smallCollectionPoints.CollectionUnitId && x.Users.Any(u => u.Role.Name == UserRole.AdminWarehouse.ToString()),
				includeProperties: "Users,Users.Role"
			);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			var adminUser = smallPoint?.Users.FirstOrDefault(u => u.Role.Name == UserRole.AdminWarehouse.ToString());
			if (adminUser == null) throw new AppException("Không tìm thấy tài khoản quản trị kho", 404);
			adminUser.Email = email;
			adminUser.Phone = phone;
			smallPoint.Name = smallCollectionPoints.Name;
			smallPoint.Address = smallCollectionPoints.Address;
			smallPoint.Latitude = smallCollectionPoints.Latitude;
			smallPoint.Longitude = smallCollectionPoints.Longitude;
			smallPoint.Status = smallCollectionPoints.Status.ToString();
			smallPoint.CompanyId = smallCollectionPoints.CompanyId;
			smallPoint.OpenTime = smallCollectionPoints.OpenTime;
			smallPoint.MaxCapacity = smallCollectionPoints.MaxCapacity;
			_unitOfWork.CollectionUnits.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> UnActiveCollectionUnit(string collectionUnitId)
		{
			var collectionUnit = await _unitOfWork.CollectionUnits.GetAsync(s => s.CollectionUnitId == collectionUnitId);
			if (collectionUnit == null) throw new AppException("Không tìm thấy kho", 404);
			collectionUnit.Status = CollectionUnitStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.CollectionUnits.Update(collectionUnit);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<bool> ActiveCollectionUnit(string collectionUnitId)
		{
			var collectionUnit = await _unitOfWork.CollectionUnits.GetAsync(s => s.CollectionUnitId == collectionUnitId);
			if (collectionUnit == null) throw new AppException("Không tìm thấy kho", 404);
			collectionUnit.Status = CollectionUnitStatus.DANG_HOAT_DONG.ToString();
			_unitOfWork.CollectionUnits.Update(collectionUnit);
			await _unitOfWork.SaveAsync();
			return true;
		}
		public async Task<byte[]> ExportSmallCollectionPointToExcelAsync(string id)
		{
			var point = await _unitOfWork.CollectionUnits.GetAsync(
				x => x.CollectionUnitId == id && x.Users.Any(u => u.Role.Name == UserRole.AdminWarehouse.ToString()),
				includeProperties: "Users,Users.Role"
			);

			if (point == null) throw new AppException("Không tìm thấy đơn vị thu gom", 404);

			var adminUser = point.Users.FirstOrDefault();

			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("CollectionUnits");

				string[] headers = {
			"STT", "Mã đơn vị thu gom", "Tên đơn vị thu gom", "Địa chỉ", "Email",
			"Số điện thoại", "Giờ mở cửa", "Sức chứa tối đa", "Mã công ty", "Tình trạng"
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

				worksheet.Cell(2, 1).Value = 1; // STT
				worksheet.Cell(2, 2).Value = point.CollectionUnitId;
				worksheet.Cell(2, 3).Value = point.Name;
				worksheet.Cell(2, 4).Value = point.Address;

				worksheet.Cell(2, 5).Value = adminUser?.Email ?? "Chưa có dữ liệu";
				worksheet.Cell(2, 6).Value = adminUser?.Phone ?? "Chưa có dữ liệu";

				worksheet.Cell(2, 7).Value = point.OpenTime;
				worksheet.Cell(2, 8).Value = point.MaxCapacity;
				worksheet.Cell(2, 9).Value = point.CompanyId;

				// Map Status theo đúng logic Import của bạn
				string statusText = "Không hoạt động";
				if (point.Status == CollectionUnitStatus.DANG_HOAT_DONG.ToString())
					statusText = "Còn hoạt động";
				else if (point.Status == CollectionUnitStatus.BAO_TRI.ToString())
					statusText = "Bảo trì";

				worksheet.Cell(2, 10).Value = statusText;

				// 4. Thêm bảng chú thích (Legend) ở cột L
				// Tạo tiêu đề bảng chú thích
				var legendHeader = worksheet.Cell(1, 12);
				legendHeader.Value = "Trạng thái hợp lệ";
				legendHeader.Style.Font.Bold = true;
				legendHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6");
				legendHeader.Style.Font.FontColor = XLColor.White;
				legendHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

				// Các giá trị hợp lệ cho cột Tình trạng
				worksheet.Cell(2, 12).Value = "Còn hoạt động";
				worksheet.Cell(3, 12).Value = "Không hoạt động";

				// Định dạng viền cho bảng chú thích
				var legendRange = worksheet.Range("L1:L4");
				legendRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				legendRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
				legendRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				worksheet.Column(12).Width = 20;

				// 5. Căn chỉnh bảng chính và định dạng
				worksheet.Columns(1, 10).AdjustToContents();
				var dataRange = worksheet.Range("A1:J2");
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

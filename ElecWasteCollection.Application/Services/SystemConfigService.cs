using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.Http;

namespace ElecWasteCollection.Application.Services
{
	public class SystemConfigService : ISystemConfigService
	{
		private readonly ISystemConfigRepository _systemConfigRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly ICloudinaryService _cloudinaryService;

		public SystemConfigService(ISystemConfigRepository systemConfigRepository, IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
		{
			_systemConfigRepository = systemConfigRepository;
			_unitOfWork = unitOfWork;
			_cloudinaryService = cloudinaryService;
		}

		public async Task<bool> CreateNewConfigWithFileAsync(IFormFile file)
		{
			string fileUrl = await _cloudinaryService.UploadRawFileAsync(file, SystemConfigKey.FORMAT_IMPORT_VEHICLE.ToString());

			var newConfig = new SystemConfig
			{
				SystemConfigId = Guid.NewGuid(),
				Key = SystemConfigKey.FORMAT_IMPORT_VEHICLE.ToString(),
				Value = fileUrl,
				DisplayName = "Mẫu phương tiện excel",
				GroupName = "Excel",
				Status = SystemConfigStatus.DANG_HOAT_DONG.ToString()
			};

			// 4. Lưu xuống DB
			await _unitOfWork.SystemConfig.AddAsync(newConfig);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<(byte[] fileBytes, string fileName)> DownloadFileByConfigIdAsync(Guid id)
		{
			// 1. Lấy thông tin từ DB
			var config = await _systemConfigRepository.GetByIdAsync(id);
			if (config == null || string.IsNullOrEmpty(config.Value))
			{
				throw new Exception("Không tìm thấy cấu hình hoặc URL file.");
			}

			// 2. Dùng HttpClient để tải file từ Cloudinary về Server
			using var httpClient = new HttpClient();
			var fileBytes = await httpClient.GetByteArrayAsync(config.Value);

			// 3. Xác định tên file (lấy từ URL hoặc dùng DisplayName)
			string fileName = Path.GetFileName(config.Value) ?? "downloaded_file.xlsx";

			return (fileBytes, fileName);
		}

		public async Task<List<SystemConfigModel>> GetAllSystemConfigActive(string? GroupName)
		{
			var activeConfigs = await _systemConfigRepository.GetsAsync(config => config.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());
			if (!string.IsNullOrEmpty(GroupName))
			{
				activeConfigs = activeConfigs.Where(c => c.GroupName == GroupName).ToList();
			}
			if (activeConfigs == null || !activeConfigs.Any())
			{
				return new List<SystemConfigModel>();
			}
			var result = activeConfigs.Select(config => new SystemConfigModel
			{
				SystemConfigId = config.SystemConfigId,
				Key = config.Key,
				Value = config.Value,
				DisplayName = config.DisplayName,
				GroupName = config.GroupName,
				Status = config.Status
				
			}).ToList();

			return result;
		}

		public async Task<SystemConfigModel> GetSystemConfigByKey(string key)
		{
			// Chuyển cả 2 vế về viết thường để so sánh
			var config = await _systemConfigRepository
				.GetAsync(c => c.Key.ToLower() == key.ToLower()
							   && c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

			if (config == null) throw new AppException("không tìm thấy config", 404);

			return new SystemConfigModel
			{
				SystemConfigId = config.SystemConfigId,
				Key = config.Key,
				Value = config.Value,
				DisplayName = config.DisplayName,
				GroupName = config.GroupName
			};
		}

		public async Task<bool> UpdateSystemConfig(UpdateSystemConfigModel model)
		{
			var config = await _systemConfigRepository
				.GetAsync(c => c.SystemConfigId == model.SystemConfigId);

			if (config == null) throw new AppException("không tìm thấy config", 404);

			if (!string.IsNullOrEmpty(model.Value))
			{
				config.Value = model.Value;
			} else if (model.ExcelFile != null)
			{
				var value = await _cloudinaryService.UploadRawFileAsync(model.ExcelFile, config.Key);
				config.Value = value;
			}
			_unitOfWork.SystemConfig.Update(config);
			await _unitOfWork.SaveAsync();
			return true;
		}

        public async Task<PagedResult<WarehouseSpeedResponse>> GetWarehouseSpeedsPagedAsync(int page, int limit, string? searchTerm)
        {
            // 1. Chỉ lấy các config ĐANG_HOAT_DONG, đúng Key và dành cho Kho
            var configs = await _systemConfigRepository.GetsAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.SmallCollectionPointId != null &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            // 2. Lọc dữ liệu theo từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                configs = configs.Where(c =>
                    (c.DisplayName != null && c.DisplayName.ToLower().Contains(searchTerm)) ||
                    c.SmallCollectionPointId.ToLower().Contains(searchTerm)
                ).ToList();
            }

            // 3. Phân trang
            var totalItems = configs.Count();
            var pagedData = configs
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(c => new WarehouseSpeedResponse
                {
                    SystemConfigId = c.SystemConfigId,
                    Key = c.Key,
                    Value = c.Value,
                    DisplayName = c.DisplayName,
                    GroupName = c.GroupName,
                    Status = c.Status,
                    SmallCollectionPointId = c.SmallCollectionPointId
                }).ToList();

            return new PagedResult<WarehouseSpeedResponse>
            {
                Page = page,
                Limit = limit,
                TotalItems = totalItems,
                Data = pagedData
            };
        }

        public async Task<bool> UpsertWarehouseSpeedAsync(WarehouseSpeedRequest model)
        {
            // Tìm cấu hình hiện tại (bao gồm cả những cái đã bị ẩn để có thể Re-active nếu cần)
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.SmallCollectionPointId == model.SmallCollectionPointId);

            if (config != null)
            {
                // Cập nhật giá trị và kích hoạt lại nếu đang ở trạng thái KHONG_HOAT_DONG
                config.Value = model.SpeedKmh.ToString();
                config.Status = SystemConfigStatus.DANG_HOAT_DONG.ToString();
                config.DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString();
                config.GroupName = "PointConfig";

                _unitOfWork.SystemConfig.Update(config);
            }
            else
            {
                // Tạo mới hoàn toàn theo format dữ liệu mẫu
                var newConfig = new SystemConfig
                {
                    SystemConfigId = Guid.NewGuid(),
                    Key = SystemConfigKey.TRANSPORT_SPEED.ToString(),
                    Value = model.SpeedKmh.ToString(),
                    DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString(),
                    GroupName = "PointConfig",
                    Status = SystemConfigStatus.DANG_HOAT_DONG.ToString(),
                    SmallCollectionPointId = model.SmallCollectionPointId
                };
                await _unitOfWork.SystemConfig.AddAsync(newConfig);
            }

            return await _unitOfWork.SaveAsync() > 0;
        }
        public async Task<bool> UpdateWarehouseSpeedAsync(WarehouseSpeedRequest model)
        {
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.SmallCollectionPointId == model.SmallCollectionPointId &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            if (config == null)
            {
                throw new AppException($"Không tìm thấy cấu hình tốc độ đang hoạt động cho kho {model.SmallCollectionPointId}", 404);
            }

            config.Value = model.SpeedKmh.ToString();

            config.DisplayName = SystemConfigKey.TRANSPORT_SPEED.ToString();
            config.GroupName = "PointConfig";

            _unitOfWork.SystemConfig.Update(config);
            return await _unitOfWork.SaveAsync() > 0;
        }

        public async Task<bool> DeleteWarehouseSpeedAsync(string smallCollectionPointId)
        {
            var config = await _systemConfigRepository.GetAsync(c =>
                c.Key == SystemConfigKey.TRANSPORT_SPEED.ToString() &&
                c.SmallCollectionPointId == smallCollectionPointId &&
                c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            if (config == null)
            {
                throw new AppException("Không tìm thấy cấu hình tốc độ cho kho này hoặc đã bị xóa trước đó", 404);
            }

            config.Status = SystemConfigStatus.KHONG_HOAT_DONG.ToString();

            _unitOfWork.SystemConfig.Update(config);
            return await _unitOfWork.SaveAsync() > 0;
        }


    }
}

using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
				Status = SystemConfigStatus.Active.ToString()
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

		public async Task<List<SystemConfigModel>> GetAllSystemConfigActive()
		{
			var activeConfigs = await _systemConfigRepository.GetsAsync(config => config.Status == SystemConfigStatus.Active.ToString());
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
							   && c.Status == SystemConfigStatus.Active.ToString());

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

		
	}
}

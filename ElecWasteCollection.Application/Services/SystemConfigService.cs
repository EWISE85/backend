using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
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

		public SystemConfigService(ISystemConfigRepository systemConfigRepository, IUnitOfWork unitOfWork)
		{
			_systemConfigRepository = systemConfigRepository;
			_unitOfWork = unitOfWork;
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
				GroupName = config.GroupName
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
				.GetAsync(c => c.SystemConfigId == model.SystemConfigId
								  && c.Status == SystemConfigStatus.Active.ToString());

			if (config == null) throw new AppException("không tìm thấy config", 404);

			config.Value = model.Value;
			_unitOfWork.SystemConfig.Update(config);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}

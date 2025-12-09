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
	public class SystemConfigService : ISystemConfigService
	{
		private readonly List<SystemConfig> systemConfigs = FakeDataSeeder.systemConfigs;
		public List<SystemConfigModel> GetAllSystemConfigActive()
		{
			var activeConfigs = systemConfigs
				.Where(config => config.Status == SystemConfigStatus.Active.ToString())
				.Select(config => new SystemConfigModel
				{
					SystemConfigId = config.SystemConfigId,
					Key = config.Key,
					Value = config.Value,
					DisplayName = config.DisplayName,
					GroupName = config.GroupName
				})
				.ToList();

			return activeConfigs;
		}

		public SystemConfigModel GetSystemConfigByKey(string key)
		{
			var config = systemConfigs
				.FirstOrDefault(c => c.Key.Equals(key, StringComparison.OrdinalIgnoreCase)
								  && c.Status == SystemConfigStatus.Active.ToString());

			if (config == null)
			{
				return null;
			}

			return new SystemConfigModel
			{
				SystemConfigId = config.SystemConfigId,
				Key = config.Key,
				Value = config.Value,
				DisplayName = config.DisplayName,
				GroupName = config.GroupName
			};
		}

		public bool UpdateSystemConfig(UpdateSystemConfigModel model)
		{
			var config = systemConfigs
				.FirstOrDefault(c => c.SystemConfigId == model.SystemConfigId
								  && c.Status == SystemConfigStatus.Active.ToString());

			if (config == null)
			{
				return false;
			}

			config.Value = model.Value;
			return true;
		}
	}
}

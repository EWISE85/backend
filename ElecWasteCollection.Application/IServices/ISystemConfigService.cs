using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ISystemConfigService
	{
		Task<List<SystemConfigModel>> GetAllSystemConfigActive();
		Task<SystemConfigModel> GetSystemConfigByKey(string key);

		Task<bool> UpdateSystemConfig(UpdateSystemConfigModel model);
	}
}

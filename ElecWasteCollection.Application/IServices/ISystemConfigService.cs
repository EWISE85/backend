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
		List<SystemConfigModel> GetAllSystemConfigActive();
		SystemConfigModel GetSystemConfigByKey(string key);

		bool UpdateSystemConfig(UpdateSystemConfigModel model);
	}
}

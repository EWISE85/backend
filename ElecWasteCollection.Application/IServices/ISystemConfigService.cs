using ElecWasteCollection.Application.Model;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ISystemConfigService
	{
		Task<List<SystemConfigModel>> GetAllSystemConfigActive(string? GroupName);
		Task<SystemConfigModel> GetSystemConfigByKey(string key);

		Task<bool> UpdateSystemConfig(UpdateSystemConfigModel model);
		Task<bool> CreateNewConfigWithFileAsync(IFormFile file);
		Task<(byte[] fileBytes, string fileName)> DownloadFileByConfigIdAsync(Guid id);
	}
}

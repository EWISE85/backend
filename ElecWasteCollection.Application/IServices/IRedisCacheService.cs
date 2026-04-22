using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IRedisCacheService
	{
		Task SetStringAsync(string key, string value, TimeSpan? expiry = null);
		Task<string?> GetStringAsync(string key);
		Task RemoveAsync(string key);
	}
}

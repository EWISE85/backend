using ElecWasteCollection.Application.IServices;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.ExternalService.Redis
{
	public class RedisCacheService : IRedisCacheService
	{
		private readonly IDatabase _db;

		public RedisCacheService(IConnectionMultiplexer redis)
		{
			_db = redis.GetDatabase();
		}

		public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
		{
			if (expiry.HasValue)
			{
				await _db.StringSetAsync(key, value, expiry.Value);
			}
			else
			{
				await _db.StringSetAsync(key, value);
			}
		}

		public async Task<string?> GetStringAsync(string key)
		{
			var value = await _db.StringGetAsync(key);
			return value.HasValue ? value.ToString() : null;
		}

		public async Task RemoveAsync(string key)
		{
			await _db.KeyDeleteAsync(key);
		}
	}
}

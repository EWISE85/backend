using ElecWasteCollection.Application.IServices;
using StackExchange.Redis;


namespace ElecWasteCollection.Infrastructure.ExternalService.Redis
{
	public class RedisConnectionManager : IConnectionManager
	{
		private readonly IDatabase _db;
		private const string Prefix = "online_user:";

		public RedisConnectionManager(IConnectionMultiplexer redis)
		{
			_db = redis.GetDatabase();
		}

		public void AddConnection(Guid userId, string connectionId)
		{
			var key = $"{Prefix}{userId}";
			_db.StringSet(key, connectionId, TimeSpan.FromHours(24));
		}

		public void RemoveConnection(Guid userId)
		{
			var key = $"{Prefix}{userId}";
			_db.KeyDelete(key);
		}

		public bool IsUserOnline(Guid userId)
		{
			var key = $"{Prefix}{userId}";
			return _db.KeyExists(key);
		}

		public string? GetConnectionId(Guid userId)
		{
			var key = $"{Prefix}{userId}";
			var value = _db.StringGet(key);
			return value.HasValue ? value.ToString() : null;
		}
	}
}

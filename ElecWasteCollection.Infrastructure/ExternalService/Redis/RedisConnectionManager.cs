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
			// 1. Lưu để tìm: UserId -> ConnectionId (Dùng để gọi điện)
			_db.StringSet($"{Prefix}{userId}", connectionId, TimeSpan.FromHours(24));

			// 2. Lưu ngược để tìm: ConnectionId -> UserId (Dùng để xóa khi ngắt kết nối)
			_db.StringSet($"conn:{connectionId}", userId.ToString(), TimeSpan.FromHours(24));
		}

		public void RemoveConnection(string connectionId)
		{
			// Tìm UserId từ ConnectionId trước khi xóa
			var userIdString = _db.StringGet($"conn:{connectionId}");

			if (!userIdString.IsNull)
			{
				// Xóa cả 2 key cho sạch Redis
				_db.KeyDelete($"{Prefix}{userIdString}");
				_db.KeyDelete($"conn:{connectionId}");
			}
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

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
	public class CollectorService : ICollectorService
	{
		private  List<User> collectors = FakeDataSeeder.users;
		private readonly IAccountService _accountService;
		public CollectorService(IAccountService accountService)
		{
			_accountService = accountService;
		}

		public Task<bool> AddNewCollector(User collector)
		{
			collectors.Add(collector);
			return Task.FromResult(true);
		}

		public async Task<ImportResult> CheckAndUpdateCollectorAsync(User collector, string collectorUsername, string password)
		{
			var result = new ImportResult();
			var existingCollector = collectors.FirstOrDefault(c => c.UserId == collector.UserId);
			if (existingCollector != null)
			{
				await UpdateCollector(collector);
			}
			else
			{
				await AddNewCollector(collector);
				var account = new Account
				{
					Username = collectorUsername,
					PasswordHash = password,
					UserId = collector.UserId
				};
				_accountService.AddNewAccount(account);
				result.Messages.Add($"Thêm thu gom viên '{collector.Name}' thành công.");

			}
			return result;
		}

		public Task<bool> DeleteCollector(Guid collectorId)
		{
			var collector = collectors.FirstOrDefault(c => c.UserId == collectorId);
			if (collector != null)
			{
				collector.Status = UserStatus.Inactive.ToString();
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		public List<CollectorResponse> GetAll()
		{
			var response = collectors.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.SmallCollectionPointId
			}).ToList();

			return response;

		}

		public CollectorResponse? GetById(Guid id)
		{
			var collector = collectors.FirstOrDefault(c => c.UserId == id);
			if (collector == null)
			{
				return null;
			}

			var response = new CollectorResponse
			{
				CollectorId = collector.UserId,
				Name = collector.Name,
				Email = collector.Email,
				Phone = collector.Phone,
				Avatar = collector.Avatar,
				SmallCollectionPointId = collector.SmallCollectionPointId
			};

			return response;
		}

		public List<CollectorResponse> GetCollectorByCompanyId(int companyId)
		{
			var response = collectors
				.Where(c => c.CollectionCompanyId == companyId && c.Role == UserRole.Collector.ToString())
				.Select(c => new CollectorResponse
				{
					CollectorId = c.UserId,
					Name = c.Name,
					Email = c.Email,
					Phone = c.Phone,
					Avatar = c.Avatar,
					SmallCollectionPointId = c.SmallCollectionPointId
				})
				.ToList();
			return response;
		}

		public List<CollectorResponse> GetCollectorByWareHouseId(int wareHouseId)
		{
			var response = collectors
				.Where(c => c.SmallCollectionPointId == wareHouseId && c.Role == UserRole.Collector.ToString())
				.Select(c => new CollectorResponse
				{
					CollectorId = c.UserId,
					Name = c.Name,
					Email = c.Email,
					Phone = c.Phone,
					Avatar = c.Avatar,
					SmallCollectionPointId = c.SmallCollectionPointId
				})
				.ToList();
			return response;
		}

		public Task<bool> UpdateCollector(User collector)
		{
			var collectorToUpdate = collectors.FirstOrDefault(c => c.UserId == collector.UserId);
			if (collectorToUpdate != null)
			{
				collectorToUpdate.Name = collector.Name;
				collectorToUpdate.Email = collector.Email;
				collectorToUpdate.Phone = collector.Phone;
				collectorToUpdate.Avatar = collector.Avatar;
				collectorToUpdate.CollectionCompanyId = collector.CollectionCompanyId;
				collectorToUpdate.SmallCollectionPointId = collector.SmallCollectionPointId;
				collectorToUpdate.Status = collector.Status;
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}
	}
}

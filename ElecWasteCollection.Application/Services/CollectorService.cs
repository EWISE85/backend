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
	public class CollectorService : ICollectorService
	{
		private readonly ICollectorRepository _collectorRepository;
		private readonly IAccountRepsitory _accountRepsitory;
		private readonly IUnitOfWork _unitOfWork;
		public CollectorService(ICollectorRepository collectorRepository, IAccountRepsitory accountRepsitory, IUnitOfWork unitOfWork)
		{
			_collectorRepository = collectorRepository;
			_accountRepsitory = accountRepsitory;
			_unitOfWork = unitOfWork;
		}

		public async Task<bool> AddNewCollector(User collector)
		{
			await _unitOfWork.Users.AddAsync(collector);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateCollectorAsync(User collector, string collectorUsername, string password)
		{
			var result = new ImportResult();
			var existingCollector = await _collectorRepository.GetAsync(c => c.UserId == collector.UserId);
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
					UserId = collector.UserId,
					IsFirstLogin = true
				};
				await _unitOfWork.Accounts.AddAsync(account);
				result.Messages.Add($"Thêm thu gom viên '{collector.Name}' thành công.");
				await _unitOfWork.SaveAsync();
			}
			return result;
		}

		public async Task<bool> DeleteCollector(Guid collectorId)
		{
			var collector = await _collectorRepository.GetAsync(c => c.UserId == collectorId);
			if (collector == null) throw new AppException("Không tìm thấy người thu gom",404);
			collector.Status = UserStatus.Inactive.ToString();
			_unitOfWork.Users.Update(collector);
			await _unitOfWork.SaveAsync();
			return true;
			
		}

		public async Task<List<CollectorResponse>> GetAll()
		{
			var collectors = await _collectorRepository.GetAllAsync(c => c.Role == UserRole.Collector.ToString());
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

		public async Task<CollectorResponse> GetById(Guid id)
		{
			var collector = await _collectorRepository.GetAsync(c => c.UserId == id);
			if (collector == null) throw new AppException("Không tìm thấy người thu gom", 404);

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

		public async Task<List<CollectorResponse>> GetCollectorByCompanyId(string companyId)
		{
			var collectores = await _collectorRepository.GetsAsync(c => c.CollectionCompanyId == companyId && c.Role == UserRole.Collector.ToString());
			var response = collectores.Select(c => new CollectorResponse
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

		public async Task<List<CollectorResponse>> GetCollectorByWareHouseId(string wareHouseId)
		{
			var collectores = await _collectorRepository.GetsAsync(c => c.SmallCollectionPointId == wareHouseId && c.Role == UserRole.Collector.ToString());
			var response = collectores.Select(c => new CollectorResponse
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

		public async Task<bool> UpdateCollector(User collector)
		{
			var collectorToUpdate = await _collectorRepository.GetAsync(c => c.UserId == collector.UserId);
			if (collectorToUpdate == null) throw new AppException("Không tìm thấy người thu gom", 404);
			
				collectorToUpdate.Name = collector.Name;
				collectorToUpdate.Email = collector.Email;
				collectorToUpdate.Phone = collector.Phone;
				collectorToUpdate.Avatar = collector.Avatar;
				collectorToUpdate.CollectionCompanyId = collector.CollectionCompanyId;
				collectorToUpdate.SmallCollectionPointId = collector.SmallCollectionPointId;
				collectorToUpdate.Status = collector.Status;
			_unitOfWork.Users.Update(collectorToUpdate);
			await _unitOfWork.SaveAsync();
			return true;
			
		}

		public async Task<PagedResultModel<CollectorResponse>> GetPagedCollectorsAsync(CollectorSearchModel model)
		{
			var (users, totalItems) = await _collectorRepository.GetPagedCollectorsAsync(
				status: model.Status,
				companyId: model.CompanyId,
				smallCollectionPointId: model.SmallCollectionId,
				page: model.Page,
				limit: model.Limit
			);

			var resultList = users.Select(c => new CollectorResponse
			{
				CollectorId = c.UserId,
				Name = c.Name,
				Email = c.Email,
				Phone = c.Phone,
				Avatar = c.Avatar,
				SmallCollectionPointId = c.SmallCollectionPointId,
			}).ToList();

			return new PagedResultModel<CollectorResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}
	}
}

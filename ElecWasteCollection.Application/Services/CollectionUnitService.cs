using DocumentFormat.OpenXml.Spreadsheet;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CollectionUnitService : ICollectionUnitService
    {
		private readonly ICollectionUnitRepository _smallCollectionRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IAccountRepsitory _accountRepository;
		public CollectionUnitService(IUnitOfWork unitOfWork, IUserRepository userRepository, IAccountRepsitory accountRepository, ICollectionUnitRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_accountRepository = accountRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}
		public async Task<bool> AddNewSmallCollectionPoint(CollectionUnit smallCollectionPoints)
		{
			await _unitOfWork.CollectionUnits.AddAsync(smallCollectionPoints);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(CollectionUnit smallCollectionPoints, string adminUsername, string adminPassword)
		{
			var result = new ImportResult();

			var existingCompany = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPoints.CollectionUnitId);
			if (existingCompany != null)
			{
				await UpdateSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Đã cập nhật thông tin kho '{smallCollectionPoints.Name}'.");
				result.IsNew = false;
			}
			else
			{
				await AddNewSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Thêm kho '{smallCollectionPoints.Name}' thành công.");
				var newAdminWarehouse = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = null,
					Name = "Admin " + smallCollectionPoints.Name,
					Role = UserRole.AdminCompany.ToString(),
					Status = UserStatus.DANG_HOAT_DONG.ToString(),
					CollectionCompanyId = smallCollectionPoints.CompanyId,
                    CollectionUnitId = smallCollectionPoints.CollectionUnitId,
				};
				await _unitOfWork.Users.AddAsync(newAdminWarehouse);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminWarehouse.UserId,
					Username = adminUsername,
					PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
					IsFirstLogin = true
				};
				await _unitOfWork.Accounts.AddAsync(adminAccount);
				result.Messages.Add($"Tạo tài khoản quản trị kho với tên đăng nhập '{adminUsername}'.");
				result.IsNew = true;
				await _unitOfWork.SaveAsync();
			}

			return result;
		}

		public async Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho",404);
			smallPoint.Status = CollectionUnitStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.CollectionUnits.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model)
		{
			var (entities, totalItems) = await _smallCollectionRepository.GetPagedAsync(
				companyId: model.CompanyId,
				status: model.Status,
				page: model.Page,
				limit: model.Limit
			);
			var resultList = entities.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return new PagedResultModel<SmallCollectionPointsResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}

		public async Task<SmallCollectionPointsResponse> GetSmallCollectionById(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			
				return new SmallCollectionPointsResponse
				{
					Id = smallPoint.CollectionUnitId,
					CompanyId = smallPoint.CompanyId,
					Name = smallPoint.Name,
					Address = smallPoint.Address,
					Latitude = smallPoint.Latitude,
					Longitude = smallPoint.Longitude,
					OpenTime = smallPoint.OpenTime,
					Status = smallPoint.Status
				};

		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointActive()
		{
			var smallPoints = await _smallCollectionRepository.GetAllAsync(s => s.Status == CollectionUnitStatus.DANG_HOAT_DONG.ToString());
			return smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
		}

		public async Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointByCompanyId(string companyId)
		{
			var smallPoints = await _smallCollectionRepository.GetsAsync(s => s.CompanyId == companyId);
			var result = smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.CollectionUnitId,
				CompanyId = point.CompanyId,
				Name = point.Name,
				Address = point.Address,
				Latitude = point.Latitude,
				Longitude = point.Longitude,
				OpenTime = point.OpenTime,
				Status = point.Status
			}).ToList();
			return result;
		}

		public async Task<bool> UpdateSmallCollectionPoint(CollectionUnit smallCollectionPoints)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.CollectionUnitId == smallCollectionPoints.CollectionUnitId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			var statusEnum = StatusEnumHelper.GetValueFromDescription<CollectionUnitStatus>(smallCollectionPoints.Status).ToString();
			smallPoint.Name = smallCollectionPoints.Name;
			smallPoint.Address = smallCollectionPoints.Address;
			smallPoint.Latitude = smallCollectionPoints.Latitude;
			smallPoint.Longitude = smallCollectionPoints.Longitude;
			smallPoint.Status = statusEnum.ToString();
			smallPoint.CompanyId = smallCollectionPoints.CompanyId;
			smallPoint.OpenTime = smallCollectionPoints.OpenTime;
			_unitOfWork.CollectionUnits.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}

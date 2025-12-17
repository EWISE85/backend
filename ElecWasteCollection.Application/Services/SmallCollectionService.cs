using DocumentFormat.OpenXml.Spreadsheet;
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
	public class SmallCollectionService : ISmallCollectionService
	{
		private readonly ISmallCollectionRepository _smallCollectionRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IUserRepository _userRepository;
		private readonly IAccountRepsitory _accountRepository;
		public SmallCollectionService(IUnitOfWork unitOfWork, IUserRepository userRepository, IAccountRepsitory accountRepository, ISmallCollectionRepository smallCollectionRepository)
		{
			_unitOfWork = unitOfWork;
			_userRepository = userRepository;
			_accountRepository = accountRepository;
			_smallCollectionRepository = smallCollectionRepository;
		}
		public async Task<bool> AddNewSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			await _unitOfWork.SmallCollectionPoints.AddAsync(smallCollectionPoints);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword)
		{
			var result = new ImportResult();

			var existingCompany = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPoints.SmallCollectionPointsId);
			if (existingCompany != null)
			{
				await UpdateSmallCollectionPoint(smallCollectionPoints);
			}
			else
			{
				await AddNewSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Thêm kho '{smallCollectionPoints.Name}' thành công.");
				var newAdminWarehouse = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = "https://example.com/default-avatar.png",
					Name = "Admin " + smallCollectionPoints.Name,
					Role = UserRole.AdminCompany.ToString(),
					CollectionCompanyId = smallCollectionPoints.CompanyId,
					SmallCollectionPointId = smallCollectionPoints.SmallCollectionPointsId,
				};
				await _unitOfWork.Users.AddAsync(newAdminWarehouse);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminWarehouse.UserId,
					Username = adminUsername,
					PasswordHash = adminPassword,
				};
				await _unitOfWork.Accounts.AddAsync(adminAccount);
				result.Messages.Add($"Tạo tài khoản quản trị kho với tên đăng nhập '{adminUsername}'.");
				await _unitOfWork.SaveAsync();
			}

			return result;
		}

		public async Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho",404);
			smallPoint.Status = SmallCollectionPointStatus.Inactive.ToString();
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
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
				Id = point.SmallCollectionPointsId,
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
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPointId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);
			
				return new SmallCollectionPointsResponse
				{
					Id = smallPoint.SmallCollectionPointsId,
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
			var smallPoints = await _smallCollectionRepository.GetAllAsync(s => s.Status == SmallCollectionPointStatus.Active.ToString());
			return smallPoints.Select(point => new SmallCollectionPointsResponse
			{
				Id = point.SmallCollectionPointsId,
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
				Id = point.SmallCollectionPointsId,
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

		public async Task<bool> UpdateSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			var smallPoint = await _smallCollectionRepository.GetAsync(s => s.SmallCollectionPointsId == smallCollectionPoints.SmallCollectionPointsId);
			if (smallPoint == null) throw new AppException("Không tìm thấy kho", 404);

			smallPoint.Name = smallCollectionPoints.Name;
			smallPoint.Address = smallCollectionPoints.Address;
			smallPoint.Latitude = smallCollectionPoints.Latitude;
			smallPoint.Longitude = smallCollectionPoints.Longitude;
			smallPoint.Status = smallCollectionPoints.Status;
			smallPoint.CompanyId = smallCollectionPoints.CompanyId;
			smallPoint.OpenTime = smallCollectionPoints.OpenTime;
			_unitOfWork.SmallCollectionPoints.Update(smallPoint);
			await _unitOfWork.SaveAsync();
			return true;
		}
	}
}

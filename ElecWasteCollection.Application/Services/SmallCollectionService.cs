using DocumentFormat.OpenXml.Spreadsheet;
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
	public class SmallCollectionService : ISmallCollectionService
	{
		private readonly List<SmallCollectionPoints> _smallCollectionPoints = FakeDataSeeder.smallCollectionPoints;
		private readonly IAccountService _accountService;
		private readonly IUserService _userService;
		public SmallCollectionService(IAccountService accountService, IUserService userService)
		{
			_accountService = accountService;
			_userService = userService;
		}
		public Task<bool> AddNewSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			_smallCollectionPoints.Add(smallCollectionPoints);
			return Task.FromResult(true);
		}

		public async Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword)
		{
			var result = new ImportResult();

			var existingCompany = _smallCollectionPoints.FirstOrDefault(s => s.Id == smallCollectionPoints.Id);
			if (existingCompany != null)
			{
				await UpdateSmallCollectionPoint(smallCollectionPoints);
			}
			else
			{
				// Nếu công ty chưa tồn tại, thêm mới
				await AddNewSmallCollectionPoint(smallCollectionPoints);
				result.Messages.Add($"Thêm kho '{smallCollectionPoints.Name}' thành công.");
				var newAdminWarehouse = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = "https://example.com/default-avatar.png",
					Name = "Admin " + smallCollectionPoints.Name,
					Role = UserRole.AdminCompany.ToString(),
					CollectionCompanyId = smallCollectionPoints.CompanyId,
					SmallCollectionPointId = smallCollectionPoints.Id,
				};
				_userService.AddUser(newAdminWarehouse);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminWarehouse.UserId,
					Username = adminUsername,
					PasswordHash = adminPassword,
				};
				_accountService.AddNewAccount(adminAccount);
			}

			return result;
		}

		public Task<bool> DeleteSmallCollectionPoint(int smallCollectionPointId)
		{
			var point = _smallCollectionPoints.FirstOrDefault(s => s.Id == smallCollectionPointId);
			if (point != null)
			{
				point.Status = SmallCollectionPointStatus.Inactive.ToString();
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		public SmallCollectionPointsResponse? GetSmallCollectionById(int smallCollectionPointId)
		{
			var point = _smallCollectionPoints.FirstOrDefault(s => s.Id == smallCollectionPointId);
			if (point != null)
			{
				return new SmallCollectionPointsResponse
				{
					Id = point.Id,
					CompanyId = point.CompanyId,
					Name = point.Name,
					Address = point.Address,
					Latitude = point.Latitude,
					Longitude = point.Longitude,
					OpenTime = point.OpenTime,
					Status = point.Status
				};
			}
			return null;
		}

		public List<SmallCollectionPointsResponse> GetSmallCollectionPointByCompanyId(int companyId)
		{
			var point = _smallCollectionPoints.FirstOrDefault(s => s.CompanyId == companyId);
			if (point != null)
			{
				return _smallCollectionPoints
					.Where(s => s.CompanyId == companyId)
					.Select(point => new SmallCollectionPointsResponse
					{
						Id = point.Id,
						CompanyId = point.CompanyId,
						Name = point.Name,
						Address = point.Address,
						Latitude = point.Latitude,
						Longitude = point.Longitude,
						OpenTime = point.OpenTime,
						Status = point.Status
					})
					.ToList();
			}
			return new List<SmallCollectionPointsResponse>();
		}

		public Task<bool> UpdateSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints)
		{
			var point = _smallCollectionPoints.FirstOrDefault(s => s.Id == smallCollectionPoints.Id);
			if (point != null)
			{
				point.Name = smallCollectionPoints.Name;
				point.Address = smallCollectionPoints.Address;
				point.Latitude = smallCollectionPoints.Latitude;
				point.Longitude = smallCollectionPoints.Longitude;
				point.Status = smallCollectionPoints.Status;
				point.CompanyId = smallCollectionPoints.CompanyId;
				point.OpenTime = smallCollectionPoints.OpenTime;
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}
	}
}

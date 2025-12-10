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
	public class CollectionCompanyService : ICollectionCompanyService
	{
		private readonly List<CollectionCompany> _teams = FakeDataSeeder.collectionTeams;
		private readonly IAccountService _accountService;
		private readonly IUserService _userService;
		public CollectionCompanyService(IAccountService accountService, IUserService userService)
		{
			_accountService = accountService;
			_userService = userService;
		}

		public Task<bool> AddNewCompany(CollectionCompany collectionTeams)
		{
			_teams.Add(collectionTeams);
			return Task.FromResult(true);
		}

		public async Task<ImportResult> CheckAndUpdateCompanyAsync(CollectionCompany collectionTeams, string adminUsername, string password)
		{
			var result = new ImportResult();

			var existingCompany = _teams.FirstOrDefault(t => t.CollectionCompanyId == collectionTeams.CollectionCompanyId);
			if (existingCompany != null)
			{
				bool isUpdated = false;

				if (existingCompany.Phone != collectionTeams.Phone)
				{
					existingCompany.Phone = collectionTeams.Phone;
					isUpdated = true;
				}
				if (existingCompany.Address != collectionTeams.Address)
				{
					existingCompany.Address = collectionTeams.Address;
					isUpdated = true;
				}
				if (existingCompany.Name != collectionTeams.Name)
				{
					existingCompany.Name = collectionTeams.Name;
					isUpdated = true;
				}

				if (existingCompany.Status != collectionTeams.Status)
				{
					existingCompany.Status = collectionTeams.Status;
					isUpdated = true;
				}

				if (isUpdated)
				{
					existingCompany.Updated_At = DateTime.UtcNow;
					// Nếu các phương thức UpdateCompany và AddNewCompany là async, thì cần await chúng
					await UpdateCompany(existingCompany);
					result.Messages.Add($"Cập nhật công ty '{collectionTeams.Name}' thành công.");
				}
				else
				{
					result.Messages.Add($"Thông tin công ty '{collectionTeams.Name}' không thay đổi.");
				}
			}
			else
			{
				// Nếu công ty chưa tồn tại, thêm mới
				await AddNewCompany(collectionTeams);
				result.Messages.Add($"Thêm công ty '{collectionTeams.Name}' thành công.");
				var newAdminCompany = new User
				{
					UserId = Guid.NewGuid(),
					Avatar = "https://example.com/default-avatar.png",
					Name = "Admin " + collectionTeams.Name,
					Email = collectionTeams.CompanyEmail,
					Role = UserRole.AdminCompany.ToString(),
					CollectionCompanyId = collectionTeams.CollectionCompanyId,
				};
				 _userService.AddUser(newAdminCompany);
				var adminAccount = new Account
				{
					AccountId = Guid.NewGuid(),
					UserId = newAdminCompany.UserId,
					Username = adminUsername,
					PasswordHash = password, // Mật khẩu mặc định, nên yêu cầu đổi sau lần đăng nhập đầu tiên
				};
				_accountService.AddNewAccount(adminAccount);
			}

			return result;
		}


		public Task<bool> DeleteCompany(string collectionCompanyId)
		{
			var team = _teams.FirstOrDefault(t => t.CollectionCompanyId == collectionCompanyId);
			if (team != null)
			{
				team.Status = CompanyStatus.Inactive.ToString();
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}

		public Task<List<CollectionCompanyResponse>> GetAllCollectionCompaniesAsync()
		{
			var response = _teams.Select(team => new CollectionCompanyResponse
			{
				Id = team.CollectionCompanyId,
				Name = team.Name,
				CompanyEmail = team.CompanyEmail,
				Phone = team.Phone,
				City = team.Address,
				Status = team.Status
			}).ToList();

			return Task.FromResult(response);
		}

		public CollectionCompanyResponse? GetCompanyById(string collectionCompanyId)
		{
			var response = _teams.FirstOrDefault(team => team.CollectionCompanyId == collectionCompanyId);
			if (response != null)
			{
				return new CollectionCompanyResponse
				{
					Id = response.CollectionCompanyId,
					Name = response.Name,
					CompanyEmail = response.CompanyEmail,
					Phone = response.Phone,
					City = response.Address,
					Status = response.Status
				};
			}
			return null;
		}

		public List<CollectionCompanyResponse>? GetCompanyByName(string companyName)
		{
			var companies = _teams
				.Where(team => team.Name.Contains(companyName, StringComparison.OrdinalIgnoreCase))
				.Select(team => new CollectionCompanyResponse
				{
					Id = team.CollectionCompanyId,
					Name = team.Name,
					CompanyEmail = team.CompanyEmail,
					Phone = team.Phone,
					City = team.Address,
					Status = team.Status
				})
				.ToList();
			return companies;
		}

		public Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model)
		{
			var query = _teams.AsQueryable();

			if (!string.IsNullOrEmpty(model.Status))
			{
				query = query.Where(c => c.Status == model.Status);
			}

			var totalItems = query.Count();
			var items = query.Skip((model.Page - 1) * model.Limit)
							 .Take(model.Limit)
							 .Select(team => new CollectionCompanyResponse
							 {
								 Id = team.CollectionCompanyId,
								 Name = team.Name,
								 CompanyEmail = team.CompanyEmail,
								 Phone = team.Phone,
								 City = team.Address,
								 Status = team.Status
							 }).ToList();

			var pagedResult = new PagedResultModel<CollectionCompanyResponse>(items, model.Page, model.Limit, totalItems);

			return Task.FromResult(pagedResult);
		}


		public Task<bool> UpdateCompany(CollectionCompany collectionTeams)
		{
			var team = _teams.FirstOrDefault(t => t.CollectionCompanyId == collectionTeams.CollectionCompanyId);
			if (team != null)
			{
				team.Address = collectionTeams.Address;
				team.CompanyEmail = collectionTeams.CompanyEmail;
				team.Name = collectionTeams.Name;
				team.Phone = collectionTeams.Phone;
				team.Status = collectionTeams.Status;
				return Task.FromResult(true);
			}
			return Task.FromResult(false);
		}
	}
}

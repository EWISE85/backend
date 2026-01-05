using DocumentFormat.OpenXml.Math;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using FirebaseAdmin.Auth.Hash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CompanyService : ICompanyService
	{
		private readonly ICompanyRepository _collectionCompanyRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAccountRepsitory _accountRepository;
		private readonly IUserRepository _userRepository;
		public CompanyService(ICompanyRepository collectionCompanyRepository, IUnitOfWork unitOfWork, IAccountRepsitory accountRepository, IUserRepository userRepository)
		{
			_collectionCompanyRepository = collectionCompanyRepository;
			_unitOfWork = unitOfWork;
			_accountRepository = accountRepository;
			_userRepository = userRepository;
		}

		public async Task<bool> AddNewCompany(Company collectionTeams)
		{
			await _unitOfWork.Companies.AddAsync(collectionTeams);
			await _unitOfWork.SaveAsync();
			return true;

		}

		public async Task<ImportResult> CheckAndUpdateCompanyAsync(Company importData, string adminUsername, string rawPassword)
		{
			var result = new ImportResult();
			if (importData == null)
			{
				result.Success = false;
				result.Messages.Add("Dữ liệu công ty trống.");
				return result;
			}

			try
			{
				var existingCompany = await _collectionCompanyRepository.GetAsync(c => c.CompanyId == importData.CompanyId);

				if (existingCompany != null)
				{
					existingCompany.Name = importData.Name;
					existingCompany.Address = importData.Address;
					existingCompany.Phone = importData.Phone;
					existingCompany.Status = importData.Status;
					existingCompany.CompanyEmail = importData.CompanyEmail;
					existingCompany.Updated_At = DateTime.UtcNow;
					_unitOfWork.Companies.Update(existingCompany);
					result.Messages.Add($"Đã cập nhật thông tin công ty '{importData.Name}'.");
				}
				else
				{
					importData.Created_At = DateTime.UtcNow;
					importData.Updated_At = DateTime.UtcNow;
					await _unitOfWork.Companies.AddAsync(importData);
					var newAdminId = Guid.NewGuid();
					var newAdminUser = new User
					{
						UserId = newAdminId,
						Name = $"Admin {importData.Name}",
						Email = importData.CompanyEmail,
						Phone = importData.Phone,
						Avatar = null,
						Role = UserRole.AdminCompany.ToString(),
						Status = UserStatus.Active.ToString(),
						CollectionCompanyId = importData.CompanyId
					};

					await _unitOfWork.Users.AddAsync(newAdminUser);
					var newAccount = new Account
					{
						AccountId = Guid.NewGuid(),
						UserId = newAdminId,
						Username = adminUsername,
						PasswordHash = rawPassword,
						IsFirstLogin = true
					};

					await _unitOfWork.Accounts.AddAsync(newAccount);
					result.Messages.Add($"Thêm mới công ty '{importData.Name}' và tài khoản Admin thành công.");
				}

				await _unitOfWork.SaveAsync();

				result.Success = true;
			}
			catch (Exception ex)
			{
				// Log lỗi (Console hoặc Logger)
				Console.WriteLine($"[ERROR] CheckAndUpdateCompanyAsync: {ex}");

				result.Success = false;
				result.Messages.Add($"Lỗi xử lý: {ex.Message}");
			}

			return result;
		}


		public async Task<bool> DeleteCompany(string collectionCompanyId)
		{
			var company = await _collectionCompanyRepository.GetAsync(t => t.CompanyId == collectionCompanyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);
			company.Status = CompanyStatus.Inactive.ToString();
			_unitOfWork.Companies.Update(company);
			await _unitOfWork.SaveAsync();
			return true;
		}

		public async Task<List<CollectionCompanyResponse>> GetAllCollectionCompaniesAsync()
		{
			var company = await _collectionCompanyRepository.GetAllAsync(filter: c => c.CompanyType == CompanyType.CollectionCompany.ToString());
			var response = company.Select(team => new CollectionCompanyResponse
			{
				Id = team.CompanyId,
				Name = team.Name,
				CompanyEmail = team.CompanyEmail,
				Phone = team.Phone,
				City = team.Address,
				Status = team.Status
			}).ToList();

			return response;
		}

		public async Task<CollectionCompanyResponse>? GetCompanyById(string collectionCompanyId)
		{
			var company = await _collectionCompanyRepository.GetAsync(c => c.CompanyId == collectionCompanyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);
			var response = new CollectionCompanyResponse
			{
				Id = company.CompanyId,
				Name = company.Name,
				CompanyEmail = company.CompanyEmail,
				Phone = company.Phone,
				City = company.Address,
				Status = company.Status
			};
			return response;
		}

		public async Task<List<CollectionCompanyResponse>> GetCompanyByName(string companyName)
		{
			var companies = await _collectionCompanyRepository.GetAllAsync(c => c.Name.Contains(companyName));
			if (companies == null) throw new AppException("Không tìm thấy công ty", 404);
			var response = companies.Select(team => new CollectionCompanyResponse
			{
				Id = team.CompanyId,
				Name = team.Name,
				CompanyEmail = team.CompanyEmail,
				Phone = team.Phone,
				City = team.Address,
				Status = team.Status
			}).ToList();
			return response;
		}

		public async Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model)
		{
			var (entities, totalItems) = await _collectionCompanyRepository.GetPagedCompaniesAsync(
				status: model.Status,
				page: model.Page,
				limit: model.Limit
			);

			var resultList = entities.Select(company => new CollectionCompanyResponse
			{
				Id = company.CompanyId,
				Name = company.Name,
				CompanyEmail = company.CompanyEmail,
				Phone = company.Phone,
				City = company.Address,
				Status = company.Status
			}).ToList();

			// 3. Đóng gói kết quả
			return new PagedResultModel<CollectionCompanyResponse>(
				resultList,
				model.Page,
				model.Limit,
				totalItems
			);
		}


		public async Task<bool> UpdateCompany(Company collectionTeams)
		{
			var team = await _collectionCompanyRepository.GetAsync(t => t.CompanyId == collectionTeams.CompanyId);
			if (team == null) throw new AppException("Không tìm thấy công ty", 404);
			team.Address = collectionTeams.Address;
			team.CompanyEmail = collectionTeams.CompanyEmail;
			team.Name = collectionTeams.Name;
			team.Phone = collectionTeams.Phone;
			team.Status = collectionTeams.Status;
			_unitOfWork.Companies.Update(team);
			await _unitOfWork.SaveAsync();
			return true;

		}
	}
}

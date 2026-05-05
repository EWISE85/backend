using DocumentFormat.OpenXml.Math;
using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.Helper;
using ClosedXML.Excel;

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

		public async Task<bool> ActiveCompany(string companyId)
		{
			var company = await _unitOfWork.Companies.GetAsync(t => t.CompanyId == companyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);
			company.Status = CompanyStatus.DANG_HOAT_DONG.ToString();
			_unitOfWork.Companies.Update(company);
			await _unitOfWork.SaveAsync();
			return true;
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
					result.IsNew = false;
				}
				else
				{
					importData.Created_At = DateTime.UtcNow;
					importData.Updated_At = DateTime.UtcNow;
					await _unitOfWork.Companies.AddAsync(importData);
					var newAdminId = Guid.NewGuid();
					var role = await _unitOfWork.Roles.GetAsync(r => r.Name == UserRole.RecyclingCompany.ToString());
					var newAdminUser = new User
					{
						UserId = newAdminId,
						Name = $"Admin {importData.Name}",
						Email = importData.CompanyEmail,
						Phone = importData.Phone,
						Avatar = null,
						RoleId = role.RoleId,
						Status = UserStatus.DANG_HOAT_DONG.ToString(),
						CompanyId = importData.CompanyId
					};

					await _unitOfWork.Users.AddAsync(newAdminUser);
					var newAccount = new Account
					{
						AccountId = Guid.NewGuid(),
						UserId = newAdminId,
						Username = adminUsername,
						PasswordHash = BCrypt.Net.BCrypt.HashPassword(rawPassword),
						IsFirstLogin = true
					};

					await _unitOfWork.Accounts.AddAsync(newAccount);
					result.Messages.Add($"Thêm mới công ty '{importData.Name}' và tài khoản Admin thành công.");
					result.IsNew = true;
				}

				await _unitOfWork.SaveAsync();

				result.Success = true;
				return result;
			}
			catch (Exception ex)
			{
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
			company.Status = CompanyStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.Companies.Update(company);
			await _unitOfWork.SaveAsync();
			return true;
		}

        public async Task<PagedResult<CollectionCompanyResponse>> GetCollectionCompaniesPagedAsync(int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var (companies, totalCount) =
                await _collectionCompanyRepository.GetPagedCollectionCompaniesAsync(page, limit);

            var collectionCompanies = companies
                .Select(c => new CollectionCompanyResponse
                {
                    Id = c.CompanyId,
                    Name = c.Name,
                    CompanyEmail = c.CompanyEmail,
                    Phone = c.Phone,
                    City = c.Address,
                    Status = StatusEnumHelper
                        .ConvertDbCodeToVietnameseName<CompanyStatus>(c.Status)
                })
                .ToList();

            return new PagedResult<CollectionCompanyResponse>
            {
                Data = collectionCompanies,
                Page = page,
                Limit = limit,
                TotalItems = totalCount
            };
        }

		public async Task<CollectionCompanyResponse> GetCompanyById(string collectionCompanyId)
		{
			var company = await _collectionCompanyRepository.GetAsync(c => c.CompanyId == collectionCompanyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);

			IEnumerable<CollectionUnit> warehousesEntity = new List<CollectionUnit>();

			// Chỉ còn loại hình tái chế, gộp logic lại cho gọn
			if (company.CompanyType == CompanyType.CTY_TAI_CHE.ToString())
			{
				warehousesEntity = await _unitOfWork.CollectionUnits.GetAllAsync(
					s => s.CompanyId == company.CompanyId &&
						 s.Status == CollectionUnitStatus.DANG_HOAT_DONG.ToString(),
					includeProperties: "Company"); // <-- Sửa thành "Company" cho khớp với Entity
			}

			var response = new CollectionCompanyResponse
			{
				Id = company.CompanyId,
				Name = company.Name,
				CompanyEmail = company.CompanyEmail,
				Phone = company.Phone,
				City = company.Address,
				Warehouses = warehousesEntity.Select(w => new SmallCollectionPointsResponse
				{
					Address = w.Address,
					Id = w.CollectionUnitId,
					Name = w.Name,
					Latitude = w.Latitude,
					Longitude = w.Longitude,
					OpenTime = w.OpenTime,
					CompanyName = w.Company?.Name,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CollectionUnitStatus>(w.Status)
				}).ToList(),
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CompanyStatus>(company.Status)
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
                Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<CompanyStatus>(team.Status)
            }).ToList();
			return response;
		}

		public async Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model)
		{
			string? statusEnum = null;
			if(model.Status != null)
			{
				 statusEnum = StatusEnumHelper.GetValueFromDescription<CompanyStatus>(model.Status).ToString();
			}
			string? typeEnum = null;
			if (model.Type != null)
			{
				typeEnum = StatusEnumHelper.GetValueFromDescription<CompanyType>(model.Type).ToString();
			}
			var (entities, totalItems) = await _collectionCompanyRepository.GetPagedCompaniesAsync(
				type: typeEnum,
				status: statusEnum,
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

		public async Task<bool> UnActiveCompany(string companyId)
		{
			var company = await _unitOfWork.Companies.GetAsync(t => t.CompanyId == companyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);
			company.Status = CompanyStatus.KHONG_HOAT_DONG.ToString();
			_unitOfWork.Companies.Update(company);
			await _unitOfWork.SaveAsync();
			return true;
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
		public async Task<byte[]> ExportCompanyToExcelAsync(string companyId)
		{
			// 1. Lấy dữ liệu công ty
			var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == companyId);
			if (company == null) throw new AppException("Không tìm thấy công ty", 404);

			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Company");

				// 2. Tạo Header (Dòng 1) theo đúng file mẫu
				string[] headers = { "STT", "Mã công ty", "Tên công ty", "Email", "Số điện thoại", "Địa chỉ", "Loại công ty", "Tình trạng" };
				for (int i = 0; i < headers.Length; i++)
				{
					var cell = worksheet.Cell(1, i + 1);
					cell.Value = headers[i];
					cell.Style.Font.Bold = true;
					cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6"); // Màu xanh giống file mẫu
					cell.Style.Font.FontColor = XLColor.White;
					cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				}

				// 3. Đổ dữ liệu (Dòng 2)
				worksheet.Cell(2, 1).Value = 1; // STT
				worksheet.Cell(2, 2).Value = company.CompanyId;
				worksheet.Cell(2, 3).Value = company.Name;
				worksheet.Cell(2, 4).Value = company.CompanyEmail;
				worksheet.Cell(2, 5).Value = company.Phone;
				worksheet.Cell(2, 6).Value = company.Address;

				worksheet.Cell(2, 7).Value = company.CompanyType == CompanyType.CTY_TAI_CHE.ToString()
											? "Công ty tái chế"
											: "Công ty thu gom";

				worksheet.Cell(2, 8).Value = company.Status == CompanyStatus.DANG_HOAT_DONG.ToString()
											? "Còn hoạt động"
											: "Ngưng hoạt động";

				// 4. Căn chỉnh định dạng
				worksheet.Columns().AdjustToContents();
				worksheet.RangeUsed().Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				worksheet.RangeUsed().Style.Border.InsideBorder = XLBorderStyleValues.Thin;

				worksheet.Cell(1, 10).Value = "Loại công ty";
				worksheet.Cell(1, 11).Value = "Trạng thái";

				// Các giá trị tùy chọn (Bắt đầu từ Hàng 2)
				worksheet.Cell(2, 10).Value = "Công ty thu gom";
				worksheet.Cell(3, 10).Value = "Công ty tái chế";

				worksheet.Cell(2, 11).Value = "Còn hoạt động";
				worksheet.Cell(3, 11).Value = "Ngưng hoạt động";
				var legendHeader = worksheet.Range("J1:K1");
				legendHeader.Style.Font.Bold = true;
				legendHeader.Style.Font.FontColor = XLColor.White;
				legendHeader.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E75B6"); 
				legendHeader.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
				legendHeader.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				legendHeader.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

				var typeBody = worksheet.Range("J2:J3");
				typeBody.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				typeBody.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
				typeBody.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2"); // Màu xanh nhạt
				typeBody.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

				// --- FORMAT VÙNG DỮ LIỆU TRẠNG THÁI (K2:K3) ---
				var statusBody = worksheet.Range("K2:K3");
				statusBody.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
				statusBody.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
				statusBody.Style.Fill.BackgroundColor = XLColor.FromHtml("#E2EFDA"); // Màu xanh lá nhạt
				statusBody.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

				// Tự động giãn độ rộng cột để nhìn rõ nội dung chú thích
				worksheet.Column(10).Width = 20;
				worksheet.Column(11).Width = 20;
				using (var stream = new MemoryStream())
				{
					workbook.SaveAs(stream);
					return stream.ToArray();
				}
			}
		}
	}
}

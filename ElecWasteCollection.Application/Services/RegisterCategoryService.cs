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
    public class RegisterCategoryService : IRegisterCategoryService
    {
        private readonly IUnitOfWork _unitOfWork;

        public RegisterCategoryService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<RegisterCategoryResponse> RegisterRecyclingCategoriesAsync(RegisterCategoryRequest request)
        {
            try
            {
                var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == request.CompanyId);

                if (company == null)
                    return new RegisterCategoryResponse { Success = false, Message = "Không tìm thấy công ty." };

                var currentLinks = await _unitOfWork.CompanyRecyclingCategories
                    .GetAllAsync(x => x.CompanyId == request.CompanyId);

                if (currentLinks != null && currentLinks.Any())
                {
                    foreach (var link in currentLinks)
                    {
                        _unitOfWork.CompanyRecyclingCategories.Delete(link);
                    }
                }

                if (request.CategoryIds != null && request.CategoryIds.Any())
                {
                    foreach (var catId in request.CategoryIds)
                    {
                        var newLink = new CompanyRecyclingCategory
                        {
                            CompanyId = request.CompanyId,
                            CategoryId = catId
                        };
                        await _unitOfWork.CompanyRecyclingCategories.AddAsync(newLink);
                    }
                }

                await _unitOfWork.SaveAsync();
                return new RegisterCategoryResponse { Success = true, Message = "Thành công", TotalRegistered = request.CategoryIds.Count };
            }
            catch (Exception ex)
            {
                return new RegisterCategoryResponse { Success = false, Message = ex.Message };
            }
        }

        public async Task<CompanyRegisteredCategoryResponse> GetRegisteredCategoryIdsAsync(string companyId)
        {
            if (string.IsNullOrWhiteSpace(companyId))
            {
                throw new ArgumentException("ID công ty không được để trống.");
            }
            var company = await _unitOfWork.Companies.GetAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy công ty với ID: {companyId}");
            }
            var links = await _unitOfWork.CompanyRecyclingCategories.GetAllAsync(
                filter: x => x.CompanyId == companyId,
                includeProperties: "Category"
            );
            var response = new CompanyRegisteredCategoryResponse
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name ?? "N/A", 
                TotalCategories = links?.Count() ?? 0,
                CategoryDetails = links?.Select(x => new CategoryDetailResponse
                {
                    CategoryId = x.CategoryId,
                    Name = x.Category?.Name ?? "Không xác định"
                }).ToList() ?? new List<CategoryDetailResponse>()
            };

            return response;
        }
        public async Task<PagedResult<CompanyListResponse>> GetAllRecyclingCompaniesAsync(int pageNumber, int pageSize)
        {
            pageNumber = pageNumber < 1 ? 1 : pageNumber;
            pageSize = pageSize < 1 ? 10 : pageSize;

            var allCompanies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString(),
                includeProperties: "CompanyRecyclingCategories"
            );

            int totalCount = allCompanies.Count();

            var pagedData = allCompanies
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CompanyListResponse
                {
                    CompanyId = c.CompanyId,
                    CompanyName = c.Name ?? "N/A",
                    TotalRegisteredCategories = c.CompanyRecyclingCategories?.Count ?? 0
                })
                .ToList();

            return new PagedResult<CompanyListResponse>
            {
                Page = pageNumber,     
                Limit = pageSize,      
                TotalItems = totalCount, 
                Data = pagedData        
            };
        }
    }
}

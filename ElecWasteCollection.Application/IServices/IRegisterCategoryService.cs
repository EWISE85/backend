using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IRegisterCategoryService
    {
        Task<RegisterCategoryResponse> RegisterRecyclingCategoriesAsync(RegisterCategoryRequest request);
        Task<CompanyRegisteredCategoryResponse> GetRegisteredCategoryIdsAsync(string companyId);
        Task<PagedResult<CompanyListResponse>> GetAllRecyclingCompaniesAsync(int pageNumber, int pageSize);
    }
}

using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductQueryService
    {
        Task<GetCompanyProductsResponse> GetCompanyProductsAsync(string companyId, DateOnly workDate);
        Task<SmallPointProductGroupDto> GetSmallPointProductsAsync(string smallPointId, DateOnly workDate);
        Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync();
        Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(string companyId);
        Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(string companyId); 
    }
}

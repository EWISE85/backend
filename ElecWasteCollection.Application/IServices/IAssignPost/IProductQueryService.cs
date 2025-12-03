using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductQueryService
    {
        Task<GetCompanyProductsResponse> GetCompanyProductsAsync(int companyId, DateOnly workDate);
        Task<SmallPointProductGroupDto> GetSmallPointProductsAsync(int smallPointId, DateOnly workDate);
        Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync();
        Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(int companyId);
        Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(int companyId); 
    }
}

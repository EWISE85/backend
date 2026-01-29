using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Application.Model.AssignPost;

namespace ElecWasteCollection.Application.IServices.IAssignPost
{
    public interface IProductQueryService
    {
        Task<GetCompanyProductsResponse> GetCompanyProductsAsync(string companyId, DateOnly workDate);
        Task<PagedSmallPointProductGroupDto>
               GetSmallPointProductsPagedAsync(
                   string smallPointId,
                   DateOnly workDate,
                   int page,
                   int limit);
        Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync();
        Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(string companyId);
        Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(string companyId);
        Task<object> GetProductIdsAtSmallPointAsync(string smallPointId, DateOnly workDate);
        Task<List<CompanyDailySummaryDto>> GetCompanySummariesByDateAsync(DateOnly workDate);
        Task<List<CompanyMetricsDto>> GetAllCompaniesDailyMetricsAsync(DateOnly workDate);
        Task<SmallPointCollectionMetricsDto> GetSmallPointProductsPagedStatusAsync(string smallPointId, DateOnly workDate, int page, int limit);
    }
}

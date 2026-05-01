using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IDashboardService
    {
        Task<DashboardSummaryModel> GetDashboardSummary(DateOnly from, DateOnly to);
        Task<DashboardSummaryModel> GetDashboardSummaryByDay(DateOnly date);
        Task<PackageDashboardResponse> GetPackageDashboardStats(string smallCollectionPointId, DateOnly from, DateOnly to);
        Task<SCPDashboardSummaryModel> GetSCPDashboardSummary(string smallCollectionPointId, DateOnly from, DateOnly to);
        Task<SCPDashboardSummaryModel> GetSCPDashboardSummaryByDay(string smallCollectionPointId, DateOnly date);
        Task<BrandDashboardResponse> GetBrandDashboardStats(string scpId, DateOnly from, DateOnly to);
        Task<BrandDashboardResponse> GetBrandDashboardStatsByDay(string scpId, DateOnly date);
        Task<BrandDashboardResponse> GetGlobalBrandDashboardStatsByDay(DateOnly date);
        Task<BrandDashboardResponse> GetGlobalBrandDashboardStats(DateOnly from, DateOnly to);
        Task<List<TopUserContributionModel>> GetTopUsers(string scpId, int top, DateOnly from, DateOnly to);
        Task<List<TopUserContributionModel>> GetGlobalTopUsers(int top, DateOnly from, DateOnly to);
        Task<List<UserProductDetailModel>> GetUserProductDetails(Guid userId);
        Task<PagedResultModel<BrandDetailItemResponse>> GetBrandDetailsAsync(string? scpId, string brandName, DateOnly from, DateOnly to, int page, int limit);
        Task<List<ScpOverdueSummaryResponse>> GetOverdueSummariesAsync();
        Task<PagedResultModel<OverdueProductItemResponse>> GetOverdueProductsPagedAsync(string scpId, int page, int limit);
        Task<PagedResultModel<CollectionUnitDashboardModel>> GetUnitsByCompanyAsync(string companyId, string? search, int page, int limit);
        Task<PagedResultModel<CompanyDashboardModel>> GetRecyclingCompaniesAsync(string? search, DateOnly from, DateOnly to, int page, int limit);
        Task<PagedResultModel<CollectionUnitDashboardModel>> GetCollectionUnitsAsync(string? search, DateOnly from, DateOnly to, int page, int limit);
    }
}

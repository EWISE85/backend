using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
    public interface IDashboardRepository
    {
        Task<int> CountUsersAsync(DateTime fromUtc, DateTime toUtc);
        Task<int> CountCompaniesAsync(DateTime fromUtc, DateTime toUtc);
        Task<int> CountProductsAsync(DateOnly from, DateOnly to);
        Task<int> CountPackagesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc);
        Task<List<DateTime>> GetPackageCreationDatesByScpIdAsync(string scpId, DateTime fromUtc, DateTime toUtc);
        Task<int> CountProductsByScpIdAsync(string scpId, DateOnly from, DateOnly to);
        Task<Dictionary<string, int>> GetProductCountsByCategoryByScpIdAsync(string scpId, DateOnly from, DateOnly to);
        Task<Dictionary<string, int>> GetProductCountsByBrandByScpIdAsync(string scpId, DateOnly from, DateOnly to);
        Task<Dictionary<string, int>> GetProductCountsByBrandAsync(DateOnly from, DateOnly to);
        Task<List<(Guid UserId, string Name, string Email, int ProductCount, double TotalPoints)>> GetTopUserStatsRawAsync(string scpId, int top, DateOnly from, DateOnly to);
        Task<List<(Guid ProductId, string CategoryName, string BrandName, string Status, double Point, DateOnly? CreateAt)>> GetUserProductDetailsRawAsync(Guid userId);
        Task<List<(Guid UserId, string Name, string Email, int ProductCount, double TotalPoints)>> GetGlobalTopUserStatsRawAsync(int top, DateOnly from, DateOnly to);
        Task<(List<(string UserName, string CategoryName, double Point, DateOnly? CollectedDate, string ScpName, string Status)> Data, int TotalCount)> GetProductDetailsByBrandPagedRawAsync(string? scpId, string brandName, DateOnly from, DateOnly to, int page, int limit);
        Task<List<(string BrandName, string CategoryName, string UserName, string UserEmail, double Point, DateOnly? CollectedDate, string ScpName, string Status)>> GetExportDataRawAsync(DateOnly from, DateOnly to);
        Task<List<(string ScpId, string ScpName, string ScheduleJson)>> GetOverdueRawDataAsync();
        Task<List<(Guid ProductId, string BrandName, string CategoryName, string UserName, string ScheduleJson, string Status)>> GetOverdueDetailRawAsync(string scpId);
        Task<(List<(string Id, string Name, string Phone, string Address, string Status, DateTime CreatedAt)> Data, int TotalCount)> GetPagedRecyclingCompaniesRawAsync(string? search, DateOnly from, DateOnly to, int page, int limit);
        Task<(List<(string Id, string Name, string Address, string Status)> Data, int TotalCount)> GetUnitsByCompanyRawAsync(string companyId, string? search, int page, int limit);
        Task<(List<(string Id, string Name, string Address, string Status, DateTime CreatedAt)> Data, int TotalCount)> GetPagedCollectionUnitsRawAsync(string? search, DateOnly from, DateOnly to, int page, int limit);

    }
}

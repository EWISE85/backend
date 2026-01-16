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
    }
}

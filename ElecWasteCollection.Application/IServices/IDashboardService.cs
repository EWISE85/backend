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
        Task<PackageDashboardResponse> GetPackageDashboardStats(string smallCollectionPointId, DateOnly from, DateOnly to);
        Task<ProductCollectDashboardResponse> GetCollectedProductStatsAsync(
                    string smallCollectionPointId,
                    DateOnly from,
                    DateOnly to);
    }
}

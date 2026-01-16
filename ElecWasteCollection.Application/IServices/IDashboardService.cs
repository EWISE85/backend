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
    }
}

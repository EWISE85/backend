using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IProductRepository _productRepository;

        public DashboardService(IDashboardRepository dashboardRepository, IProductRepository productRepository)
        {
            _dashboardRepository = dashboardRepository;
            _productRepository = productRepository;
        }

        public async Task<DashboardSummaryModel> GetDashboardSummary(DateOnly from, DateOnly to)
        {
            DateTime currentFromDate = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            DateTime currentToDate = DateTime.SpecifyKind(to.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);
            DateTime prevFromDate = currentFromDate.AddMonths(-1);
            DateTime prevToDate = currentToDate.AddMonths(-1);
            DateOnly prevFromDateOnly = DateOnly.FromDateTime(prevFromDate);
            DateOnly prevToDateOnly = DateOnly.FromDateTime(prevToDate);

            var currUsers = await _dashboardRepository.CountUsersAsync(currentFromDate, currentToDate);
            var prevUsers = await _dashboardRepository.CountUsersAsync(prevFromDate, prevToDate);
            var currComp = await _dashboardRepository.CountCompaniesAsync(currentFromDate, currentToDate);
            var prevComp = await _dashboardRepository.CountCompaniesAsync(prevFromDate, prevToDate);
            var currProd = await _dashboardRepository.CountProductsAsync(from, to);
            var prevProd = await _dashboardRepository.CountProductsAsync(prevFromDateOnly, prevToDateOnly);
            var currCatDict = await _productRepository.GetProductCountsByCategoryAsync(currentFromDate, currentToDate);
            var prevCatDict = await _productRepository.GetProductCountsByCategoryAsync(prevFromDate, prevToDate);

            var categoryStats = ProcessCategoryStats(currCatDict, prevCatDict);

            return new DashboardSummaryModel
            {
                FromDate = from,
                ToDate = to,
                TotalUsers = CalculateMetric(currUsers, prevUsers),
                TotalCompanies = CalculateMetric(currComp, prevComp),
                TotalProducts = CalculateMetric(currProd, prevProd),
                ProductCategories = categoryStats
            };
        }

        public async Task<DashboardSummaryModel> GetDashboardSummaryByDay(DateOnly date)
        {
            DateTime currentFromDate = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            DateTime currentToDate = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);

            DateTime prevFromDate = currentFromDate.AddDays(-1);
            DateTime prevToDate = currentToDate.AddDays(-1);
            DateOnly prevDateOnly = date.AddDays(-1);

            var currUsers = await _dashboardRepository.CountUsersAsync(currentFromDate, currentToDate);
            var prevUsers = await _dashboardRepository.CountUsersAsync(prevFromDate, prevToDate);

            var currComp = await _dashboardRepository.CountCompaniesAsync(currentFromDate, currentToDate);
            var prevComp = await _dashboardRepository.CountCompaniesAsync(prevFromDate, prevToDate);

            var currProd = await _dashboardRepository.CountProductsAsync(date, date);
            var prevProd = await _dashboardRepository.CountProductsAsync(prevDateOnly, prevDateOnly);

            var currCatDict = await _productRepository.GetProductCountsByCategoryAsync(currentFromDate, currentToDate);
            var prevCatDict = await _productRepository.GetProductCountsByCategoryAsync(prevFromDate, prevToDate);

            var categoryStats = ProcessCategoryStats(currCatDict, prevCatDict);

            return new DashboardSummaryModel
            {
                FromDate = date,
                ToDate = date,
                TotalUsers = CalculateMetric(currUsers, prevUsers),
                TotalCompanies = CalculateMetric(currComp, prevComp),
                TotalProducts = CalculateMetric(currProd, prevProd),
                ProductCategories = categoryStats
            };
        }


        public async Task<PackageDashboardResponse> GetPackageDashboardStats(string smallCollectionPointId, DateOnly from, DateOnly to)
        {
            int timezoneOffset = 7;
            var cleanId = smallCollectionPointId.Trim();

            DateTime fromDateVN = from.ToDateTime(TimeOnly.MinValue);
            DateTime toDateVN = to.ToDateTime(new TimeOnly(23, 59, 59));

            DateTime currentFromUtc = DateTime.SpecifyKind(fromDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);
            DateTime currentToUtc = DateTime.SpecifyKind(toDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);

            DateTime prevMonthFromUtc = currentFromUtc.AddMonths(-1);
            DateTime prevMonthToUtc = currentToUtc.AddMonths(-1);

            var totalCurrent = await _dashboardRepository.CountPackagesByScpIdAsync(cleanId, currentFromUtc, currentToUtc);
            var totalPrevious = await _dashboardRepository.CountPackagesByScpIdAsync(cleanId, prevMonthFromUtc, prevMonthToUtc);

            var rawData = await _dashboardRepository.GetPackageCreationDatesByScpIdAsync(cleanId, currentFromUtc, currentToUtc);

            var groupedData = rawData
                .Select(d => d.AddHours(timezoneOffset))
                .GroupBy(d => DateOnly.FromDateTime(d))
                .Select(g => new PackageDailyStat
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .ToList();

            var dailyStats = FillMissingDates(from, to, groupedData);

            for (int i = 0; i < dailyStats.Count; i++)
            {
                if (i == 0)
                {
                    dailyStats[i].PercentChange = null;
                    dailyStats[i].AbsoluteChange = null;
                    continue;
                }

                var prevDayCount = dailyStats[i - 1].Count;
                var currDayCount = dailyStats[i].Count;

                dailyStats[i].AbsoluteChange = currDayCount - prevDayCount;

                if (prevDayCount == 0)
                {
                    dailyStats[i].PercentChange = currDayCount > 0 ? 100 : 0;
                }
                else
                {
                    var percent = ((double)(currDayCount - prevDayCount) / prevDayCount) * 100;
                    dailyStats[i].PercentChange = Math.Round(percent, 1);
                }
            }

            return new PackageDashboardResponse
            {
                SmallCollectionPointId = smallCollectionPointId,
                FromDate = from,
                ToDate = to,
                TotalPackages = CalculateMetric(totalCurrent, totalPrevious),
                DailyStats = dailyStats
            };
        }

        public async Task<SCPDashboardSummaryModel> GetSCPDashboardSummary(string smallCollectionPointId, DateOnly from, DateOnly to)
        {
            var cleanId = smallCollectionPointId.Trim();

            DateOnly prevFromDate = from.AddMonths(-1);
            DateOnly prevToDate = to.AddMonths(-1);

            var currProd = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, from, to);
            var prevProd = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, prevFromDate, prevToDate);

            var currCatDict = await _dashboardRepository.GetProductCountsByCategoryByScpIdAsync(cleanId, from, to);
            var prevCatDict = await _dashboardRepository.GetProductCountsByCategoryByScpIdAsync(cleanId, prevFromDate, prevToDate);

            var categoryStats = ProcessCategoryStats(currCatDict, prevCatDict);

            return new SCPDashboardSummaryModel
            {
                SmallCollectionPointId = cleanId,
                FromDate = from,
                ToDate = to,
                TotalProducts = CalculateMetric(currProd, prevProd),
                ProductCategories = categoryStats
            };
        }

        public async Task<SCPDashboardSummaryModel> GetSCPDashboardSummaryByDay(string smallCollectionPointId, DateOnly date)
        {
            var cleanId = smallCollectionPointId.Trim();
            DateOnly prevDate = date.AddDays(-1);

            var currProd = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, date, date);
            var prevProd = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, prevDate, prevDate);

            var currCatDict = await _dashboardRepository.GetProductCountsByCategoryByScpIdAsync(cleanId, date, date);
            var prevCatDict = await _dashboardRepository.GetProductCountsByCategoryByScpIdAsync(cleanId, prevDate, prevDate);

            var categoryStats = ProcessCategoryStats(currCatDict, prevCatDict);

            return new SCPDashboardSummaryModel
            {
                SmallCollectionPointId = cleanId,
                FromDate = date,
                ToDate = date,
                TotalProducts = CalculateMetric(currProd, prevProd),
                ProductCategories = categoryStats
            };
        }

        private List<CategoryStatisticExtendedModel> ProcessCategoryStats(Dictionary<string, int> currDict, Dictionary<string, int> prevDict)
        {
            var allCategories = currDict.Keys.Union(prevDict.Keys).Distinct();
            var categoryStats = new List<CategoryStatisticExtendedModel>();

            foreach (var catName in allCategories)
            {
                int currVal = currDict.GetValueOrDefault(catName, 0);
                int prevVal = prevDict.GetValueOrDefault(catName, 0);
                var stat = CalculateMetric(currVal, prevVal);

                categoryStats.Add(new CategoryStatisticExtendedModel
                {
                    CategoryName = catName,
                    CurrentValue = stat.CurrentValue,
                    PreviousValue = stat.PreviousValue,
                    AbsoluteChange = stat.AbsoluteChange,
                    PercentChange = stat.PercentChange,
                    Trend = stat.Trend
                });
            }
            return categoryStats;
        }

        private MetricStats CalculateMetric(int current, int previous)
        {
            int diff = current - previous;
            double percent = 0;

            if (previous == 0)
            {
                percent = current > 0 ? 100 : 0;
            }
            else
            {
                percent = Math.Round(((double)diff / previous) * 100, 2);
            }

            return new MetricStats
            {
                CurrentValue = current,
                PreviousValue = previous,
                AbsoluteChange = diff,
                PercentChange = percent,
                Trend = diff > 0 ? "Increase" : (diff < 0 ? "Decrease" : "Stable")
            };
        }

        private List<PackageDailyStat> FillMissingDates(DateOnly from, DateOnly to, List<PackageDailyStat> data)
        {
            var result = new List<PackageDailyStat>();
            for (var date = from; date <= to; date = date.AddDays(1))
            {
                var existing = data.FirstOrDefault(x => x.Date == date);
                result.Add(new PackageDailyStat
                {
                    Date = date,
                    Count = existing != null ? existing.Count : 0,
                    PercentChange = null,
                    AbsoluteChange = null
                });
            }
            return result;
        }
    }

    //public async Task<DashboardSummaryModel> GetDashboardSummary(DateOnly from, DateOnly to)
    //{
    //	DateTime fromDate = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    //	DateTime toDate = DateTime.SpecifyKind(to.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);

    //	var totalUsers = await _unitOfWork.Users.CountAsync(u => u.CreateAt >= fromDate && u.CreateAt <= toDate);

    //	var totalCompanies = await _unitOfWork.Companies.CountAsync(c => c.Created_At >= fromDate && c.Created_At <= toDate);

    //	var totalProducts = await _unitOfWork.Products.CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);

    //	var statsDict = await _productRepository.GetProductCountsByCategoryAsync(fromDate, toDate);

    //	var productByCategory = statsDict.Select(kvp => new CategoryStatisticModel
    //	{
    //		CategoryName = kvp.Key,
    //		Count = kvp.Value
    //	}).ToList();

    //	return new DashboardSummaryModel
    //	{
    //		FromDate = from, 
    //		ToDate = to,
    //		TotalUsers = totalUsers,
    //		TotalCompanies = totalCompanies,
    //		TotalProducts = totalProducts,
    //		ProductCategories = productByCategory
    //	};
    //}

    //public async Task<PackageDashboardResponse> GetPackageDashboardStats(string smallCollectionPointId, DateOnly from, DateOnly to)
    //{
    //    int timezoneOffset = 7; 

    //    DateTime fromDateVN = from.ToDateTime(TimeOnly.MinValue); 
    //    DateTime toDateVN = to.ToDateTime(new TimeOnly(23, 59, 59)); 
    //    DateTime fromDateUtc = DateTime.SpecifyKind(fromDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);
    //    DateTime toDateUtc = DateTime.SpecifyKind(toDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);

    //    var query = _unitOfWork.Packages.GetQueryable()
    //        .Where(p => p.SmallCollectionPointsId.Trim() == smallCollectionPointId.Trim()
    //                    && p.CreateAt >= fromDateUtc
    //                    && p.CreateAt <= toDateUtc); 

    //    var data = await query.Select(p => p.CreateAt).ToListAsync();

    //    var totalPackages = data.Count;

    //    var dailyStats = data
    //        .Select(d => d.AddHours(timezoneOffset)) 
    //        .GroupBy(d => DateOnly.FromDateTime(d))
    //        .Select(g => new PackageDailyStat
    //        {
    //            Date = g.Key,
    //            Count = g.Count()
    //        })
    //        .OrderBy(x => x.Date)
    //        .ToList();

    //    for (int i = 0; i < dailyStats.Count; i++)
    //    {
    //        if (i == 0)
    //        {
    //            dailyStats[i].PercentChange = null;
    //            continue;
    //        }

    //        var previousCount = dailyStats[i - 1].Count;
    //        var currentCount = dailyStats[i].Count;

    //        if (previousCount == 0)
    //        {
    //            dailyStats[i].PercentChange = null;
    //        }
    //        else
    //        {
    //            var percent = ((double)(currentCount - previousCount) / previousCount) * 100;
    //            dailyStats[i].PercentChange = Math.Round(percent, 1);
    //        }
    //    }

    //    return new PackageDashboardResponse
    //    {
    //        SmallCollectionPointId = smallCollectionPointId,
    //        FromDate = from,
    //        ToDate = to,
    //        TotalPackages = totalPackages,
    //        DailyStats = dailyStats
    //    };
    //}
}

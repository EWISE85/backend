using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _dashboardRepository;
        private readonly IProductRepository _productRepository;
        private readonly DateOnly _today = DateOnly.FromDateTime(DateTime.Now);

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
        public async Task<BrandDashboardResponse> GetBrandDashboardStats(string scpId, DateOnly from, DateOnly to)
        {
            var cleanId = scpId.Trim();
            DateOnly prevFromDate = from.AddMonths(-1);
            DateOnly prevToDate = to.AddMonths(-1);

            var currTotal = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, from, to);
            var prevTotal = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, prevFromDate, prevToDate);

            var currBrandDict = await _dashboardRepository.GetProductCountsByBrandByScpIdAsync(cleanId, from, to);
            var prevBrandDict = await _dashboardRepository.GetProductCountsByBrandByScpIdAsync(cleanId, prevFromDate, prevToDate);

            return new BrandDashboardResponse
            {
                SmallCollectionPointId = cleanId,
                FromDate = from,
                ToDate = to,
                TotalProducts = CalculateMetric(currTotal, prevTotal),
                Brands = ProcessBrandStats(currBrandDict, prevBrandDict)
            };
        }

        public async Task<BrandDashboardResponse> GetBrandDashboardStatsByDay(string scpId, DateOnly date)
        {
            var cleanId = scpId.Trim();
            DateOnly prevDate = date.AddDays(-1);

            var currTotal = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, date, date);
            var prevTotal = await _dashboardRepository.CountProductsByScpIdAsync(cleanId, prevDate, prevDate);

            var currBrandDict = await _dashboardRepository.GetProductCountsByBrandByScpIdAsync(cleanId, date, date);
            var prevBrandDict = await _dashboardRepository.GetProductCountsByBrandByScpIdAsync(cleanId, prevDate, prevDate);

            return new BrandDashboardResponse
            {
                SmallCollectionPointId = cleanId,
                FromDate = date,
                ToDate = date,
                TotalProducts = CalculateMetric(currTotal, prevTotal),
                Brands = ProcessBrandStats(currBrandDict, prevBrandDict)
            };
        }

        private List<BrandStatisticExtendedModel> ProcessBrandStats(Dictionary<string, int> currDict, Dictionary<string, int> prevDict)
        {
            var allBrands = currDict.Keys.Union(prevDict.Keys).Distinct();
            var results = new List<BrandStatisticExtendedModel>();

            foreach (var name in allBrands)
            {
                int currVal = currDict.GetValueOrDefault(name, 0);
                int prevVal = prevDict.GetValueOrDefault(name, 0);

                var metric = CalculateMetric(currVal, prevVal);

                results.Add(new BrandStatisticExtendedModel
                {
                    BrandName = name,
                    CurrentValue = metric.CurrentValue,
                    PreviousValue = metric.PreviousValue,
                    AbsoluteChange = metric.AbsoluteChange,
                    PercentChange = metric.PercentChange,
                    Trend = metric.Trend
                });
            }
            return results.OrderByDescending(x => x.CurrentValue).ToList();
        }

        public async Task<List<TopUserContributionModel>> GetTopUsers(string scpId, int top, DateOnly from, DateOnly to)
        {
            var raw = await _dashboardRepository.GetTopUserStatsRawAsync(scpId, top, from, to);
            return raw.Select(x => new TopUserContributionModel
            {
                UserId = x.UserId,
                Name = x.Name,
                Email = x.Email,
                TotalProducts = x.ProductCount,
                TotalPoints = x.TotalPoints
            }).ToList();
        }
        public async Task<List<TopUserContributionModel>> GetGlobalTopUsers(int top, DateOnly from, DateOnly to)
        {
            var raw = await _dashboardRepository.GetGlobalTopUserStatsRawAsync(top, from, to);

            return raw.Select(x => new TopUserContributionModel
            {
                UserId = x.UserId,
                Name = x.Name,
                Email = x.Email,
                TotalProducts = x.ProductCount,
                TotalPoints = x.TotalPoints
            }).ToList();
        }

        public async Task<List<UserProductDetailModel>> GetUserProductDetails(Guid userId)
        {
            var raw = await _dashboardRepository.GetUserProductDetailsRawAsync(userId);
            return raw.Select(x => new UserProductDetailModel
            {
                ProductId = x.ProductId,
                ProductName = x.CategoryName,
                BrandName = x.BrandName,
                Status = x.Status,
                Point = x.Point,
                CreateAt = x.CreateAt
            }).ToList();
        }
        public async Task<BrandDashboardResponse> GetGlobalBrandDashboardStats(DateOnly from, DateOnly to)
        {
            DateOnly prevFromDate = from.AddMonths(-1);
            DateOnly prevToDate = to.AddMonths(-1);

            var currTotal = await _dashboardRepository.CountProductsAsync(from, to);
            var prevTotal = await _dashboardRepository.CountProductsAsync(prevFromDate, prevToDate);

            var currBrandDict = await _dashboardRepository.GetProductCountsByBrandAsync(from, to);
            var prevBrandDict = await _dashboardRepository.GetProductCountsByBrandAsync(prevFromDate, prevToDate);

            return new BrandDashboardResponse
            {
                SmallCollectionPointId = "ALL",
                FromDate = from,
                ToDate = to,
                TotalProducts = CalculateMetric(currTotal, prevTotal),
                Brands = ProcessBrandStats(currBrandDict, prevBrandDict)
            };
        }

        public async Task<BrandDashboardResponse> GetGlobalBrandDashboardStatsByDay(DateOnly date)
        {
            DateOnly prevDate = date.AddDays(-1);

            var currTotal = await _dashboardRepository.CountProductsAsync(date, date);
            var prevTotal = await _dashboardRepository.CountProductsAsync(prevDate, prevDate);

            var currBrandDict = await _dashboardRepository.GetProductCountsByBrandAsync(date, date);
            var prevBrandDict = await _dashboardRepository.GetProductCountsByBrandAsync(prevDate, prevDate);

            return new BrandDashboardResponse
            {
                SmallCollectionPointId = "ALL",
                FromDate = date,
                ToDate = date,
                TotalProducts = CalculateMetric(currTotal, prevTotal),
                Brands = ProcessBrandStats(currBrandDict, prevBrandDict)
            };
        }
        public async Task<PagedResultModel<BrandDetailItemResponse>> GetBrandDetailsAsync(string? scpId, string brandName, DateOnly from, DateOnly to, int page, int limit)
        {
            var (rawData, totalItems) = await _dashboardRepository.GetProductDetailsByBrandPagedRawAsync(scpId, brandName, from, to, page, limit);

            var data = rawData.Select(x => new BrandDetailItemResponse
            {
                UserName = x.Item1,
                CategoryName = x.Item2,
                Point = x.Item3,
                CollectedDate = x.Item4,
                ScpName = x.Item5
            }).ToList();

            return new PagedResultModel<BrandDetailItemResponse>(data, page, limit, totalItems);
        }
        public async Task<List<ScpOverdueSummaryResponse>> GetOverdueSummariesAsync()
        {
            var rawData = await _dashboardRepository.GetOverdueRawDataAsync();

            return rawData
                .Select(x => new { x.ScpId, x.ScpName, Deadline = GetMaxDate(x.ScheduleJson) })
                .Where(x => x.Deadline.HasValue && x.Deadline < _today)
                .GroupBy(x => new { x.ScpId, x.ScpName })
                .Select(g => new ScpOverdueSummaryResponse
                {
                    ScpId = g.Key.ScpId,
                    ScpName = g.Key.ScpName,
                    TotalOverdueCount = g.Count()
                })
                .OrderByDescending(x => x.TotalOverdueCount)
                .ToList();
        }

        public async Task<PagedResultModel<OverdueProductItemResponse>> GetOverdueProductsPagedAsync(string scpId, int page, int limit)
        {
            var rawData = await _dashboardRepository.GetOverdueDetailRawAsync(scpId);

            var allOverdue = rawData
                .Select(x => {
                    var deadline = GetMaxDate(x.ScheduleJson);
                    return new OverdueProductItemResponse
                    {
                        ProductId = x.ProductId,
                        BrandName = x.BrandName,
                        CategoryName = x.CategoryName,
                        UserName = x.UserName,
                        DeadlineDate = deadline,
                        DaysDelayed = deadline.HasValue ? _today.DayNumber - deadline.Value.DayNumber : 0,
                        Status = x.Status
                    };
                })
                .Where(x => x.DeadlineDate.HasValue && x.DeadlineDate < _today)
                .OrderByDescending(x => x.DaysDelayed)
                .ToList();

            var pagedData = allOverdue.Skip((page - 1) * limit).Take(limit).ToList();
            return new PagedResultModel<OverdueProductItemResponse>(pagedData, page, limit, allOverdue.Count);
        }

        private DateOnly? GetMaxDate(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var slots = JsonSerializer.Deserialize<List<ScheduleSlot>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return slots?.Max(s => s.PickUpDate);
            }
            catch { return null; }
        }
    }
}

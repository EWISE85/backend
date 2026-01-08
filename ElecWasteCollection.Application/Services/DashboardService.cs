using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.EntityFrameworkCore;

namespace ElecWasteCollection.Application.Services
{
	public class DashboardService : IDashboardService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductRepository _productRepository;
		public DashboardService(IUnitOfWork unitOfWork, IProductRepository productRepository)
		{
			_unitOfWork = unitOfWork;
			_productRepository = productRepository;
		}
		public async Task<DashboardSummaryModel> GetDashboardSummary(DateOnly from, DateOnly to)
		{
			DateTime fromDate = DateTime.SpecifyKind(from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

			DateTime toDate = DateTime.SpecifyKind(to.ToDateTime(new TimeOnly(23, 59, 59)), DateTimeKind.Utc);

			var totalUsers = await _unitOfWork.Users.CountAsync(u => u.CreateAt >= fromDate && u.CreateAt <= toDate);

			var totalCompanies = await _unitOfWork.Companies.CountAsync(c => c.Created_At >= fromDate && c.Created_At <= toDate);

			var totalProducts = await _unitOfWork.Products.CountAsync(p => p.CreateAt >= from && p.CreateAt <= to);

			var statsDict = await _productRepository.GetProductCountsByCategoryAsync(fromDate, toDate);

			var productByCategory = statsDict.Select(kvp => new CategoryStatisticModel
			{
				CategoryName = kvp.Key,
				Count = kvp.Value
			}).ToList();

			return new DashboardSummaryModel
			{
				FromDate = from, 
				ToDate = to,
				TotalUsers = totalUsers,
				TotalCompanies = totalCompanies,
				TotalProducts = totalProducts,
				ProductCategories = productByCategory
			};
		}

        public async Task<PackageDashboardResponse> GetPackageDashboardStats(string smallCollectionPointId, DateOnly from, DateOnly to)
        {
            int timezoneOffset = 7; 

            DateTime fromDateVN = from.ToDateTime(TimeOnly.MinValue); 
            DateTime toDateVN = to.ToDateTime(new TimeOnly(23, 59, 59)); 
            DateTime fromDateUtc = DateTime.SpecifyKind(fromDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);
            DateTime toDateUtc = DateTime.SpecifyKind(toDateVN.AddHours(-timezoneOffset), DateTimeKind.Utc);

            var query = _unitOfWork.Packages.GetQueryable()
                .Where(p => p.SmallCollectionPointsId.Trim() == smallCollectionPointId.Trim()
                            && p.CreateAt >= fromDateUtc
                            && p.CreateAt <= toDateUtc); 

            var data = await query.Select(p => p.CreateAt).ToListAsync();

            var totalPackages = data.Count;

            var dailyStats = data
                .Select(d => d.AddHours(timezoneOffset)) 
                .GroupBy(d => DateOnly.FromDateTime(d))
                .Select(g => new PackageDailyStat
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            for (int i = 0; i < dailyStats.Count; i++)
            {
                if (i == 0)
                {
                    dailyStats[i].PercentChange = null;
                    continue;
                }

                var previousCount = dailyStats[i - 1].Count;
                var currentCount = dailyStats[i].Count;

                if (previousCount == 0)
                {
                    dailyStats[i].PercentChange = null;
                }
                else
                {
                    var percent = ((double)(currentCount - previousCount) / previousCount) * 100;
                    dailyStats[i].PercentChange = Math.Round(percent, 1);
                }
            }

            return new PackageDashboardResponse
            {
                SmallCollectionPointId = smallCollectionPointId,
                FromDate = from,
                ToDate = to,
                TotalPackages = totalPackages,
                DailyStats = dailyStats
            };
        }

        public async Task<ProductCollectDashboardResponse> GetCollectedProductStatsAsync( string smallCollectionPointId, DateOnly from, DateOnly to)
        {
            var fromUtc = DateTime.SpecifyKind( from.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            var toUtc = DateTime.SpecifyKind( to.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);

            var collectedProducts = await (
                from h in _unitOfWork.ProductStatusHistory.GetQueryable().AsNoTracking()
                join p in _unitOfWork.Products.GetQueryable().AsNoTracking()
                    on h.ProductId equals p.ProductId
                where h.Status == "Đã thu gom"
                      && h.ChangedAt >= fromUtc
                      && h.ChangedAt <= toUtc
                      && p.SmallCollectionPointId == smallCollectionPointId
                select new
                {
                    p.ProductId,
                    Date = DateOnly.FromDateTime(h.ChangedAt)
                }
            )
            .Distinct()
            .ToListAsync();

            if (!collectedProducts.Any())
            {
                return EmptyResult(smallCollectionPointId, from, to);
            }

            var productStats = new List<(DateOnly date, double weight, double volume)>();

            foreach (var item in collectedProducts)
            {
                var att = await GetProductAttributesAsync(item.ProductId);
                productStats.Add((item.Date, att.weight, att.volume));
            }

            var dailyStats = productStats
                .GroupBy(x => x.date)
                .Select(g => new ProductCollectDailyStat
                {
                    Date = g.Key,
                    TotalWeightKg = Math.Round(g.Sum(x => x.weight), 2),
                    TotalVolumeM3 = Math.Round(g.Sum(x => x.volume), 4)
                })
                .OrderBy(x => x.Date)
                .ToList();

            for (int i = 1; i < dailyStats.Count; i++)
            {
                var prev = dailyStats[i - 1];
                var cur = dailyStats[i];

                cur.WeightPercentChange = prev.TotalWeightKg > 0
                    ? Math.Round(((cur.TotalWeightKg - prev.TotalWeightKg) / prev.TotalWeightKg) * 100, 1)
                    : (cur.TotalWeightKg > 0 ? 100 : 0);

                cur.VolumePercentChange = prev.TotalVolumeM3 > 0
                    ? Math.Round(((cur.TotalVolumeM3 - prev.TotalVolumeM3) / prev.TotalVolumeM3) * 100, 1)
                    : (cur.TotalVolumeM3 > 0 ? 100 : 0);
            }

            return new ProductCollectDashboardResponse
            {
                SmallCollectionPointId = smallCollectionPointId,
                FromDate = from,
                ToDate = to,
                TotalWeightKg = Math.Round(productStats.Sum(x => x.weight), 2),
                TotalVolumeM3 = Math.Round(productStats.Sum(x => x.volume), 4),
                DailyStats = dailyStats
            };
        }

        private ProductCollectDashboardResponse EmptyResult(
            string scpId, DateOnly from, DateOnly to)
        {
            return new ProductCollectDashboardResponse
            {
                SmallCollectionPointId = scpId,
                FromDate = from,
                ToDate = to,
                TotalWeightKg = 0,
                TotalVolumeM3 = 0,
                DailyStats = new List<ProductCollectDailyStat>()
            };
        }

        private async Task<(double length, double width, double height, double weight, double volume, string dimensionText)> GetProductAttributesAsync(Guid productId)
        {
            var pValues = await _unitOfWork.ProductValues.GetAllAsync(v => v.ProductId == productId);
            var pValuesList = pValues.ToList();

            double weight = 0;
            double volume = 0;
            double length = 0;
            double width = 0;
            double height = 0;
            string dimText = "";

            foreach (var val in pValuesList)
            {
                if (val.AttributeOptionId.HasValue)
                {
                    var option = await _unitOfWork.AttributeOptions.GetByIdAsync(val.AttributeOptionId.Value);
                    if (option != null)
                    {
                        if (option.EstimateWeight.HasValue && option.EstimateWeight.Value > 0)
                        {
                            weight = option.EstimateWeight.Value;
                            if (string.IsNullOrEmpty(dimText)) dimText = option.OptionName;
                        }

                        if (option.EstimateVolume.HasValue && option.EstimateVolume.Value > 0)
                        {
                            volume = option.EstimateVolume.Value;
                            dimText = option.OptionName; 
                        }
                    }
                }
                else if (val.Value.HasValue && val.Value.Value > 0)
                {
                    var attribute = await _unitOfWork.Attributes.GetByIdAsync(val.AttributeId);
                    if (attribute != null)
                    {
                        string nameLower = attribute.Name.ToLower();
                        if (nameLower.Contains("dài") || nameLower.Contains("length")) length = val.Value.Value;
                        else if (nameLower.Contains("rộng") || nameLower.Contains("width")) width = val.Value.Value;
                        else if (nameLower.Contains("cao") || nameLower.Contains("height")) height = val.Value.Value;
                    }
                }
            }

            if (volume <= 0 && length > 0 && width > 0 && height > 0)
            {
                volume = (length * width * height) / 1_000_000.0; 
                dimText = $"{length} x {width} x {height} cm";
            }

            if (weight <= 0) weight = 1;
            if (volume <= 0)
            {
                volume = 0.001;
                if (string.IsNullOrEmpty(dimText)) dimText = "Không xác định";
            }
            else if (string.IsNullOrEmpty(dimText))
            {
                dimText = $"~ {Math.Round(volume, 3)} m3";
            }

            return (length, width, height, weight, volume, dimText);
        }


    }
}

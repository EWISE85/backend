using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using System.Text.Json;
using ElecWasteCollection.Domain.Entities;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductAssignService : IProductAssignService
    {
        private readonly IMapboxDistanceCacheService _distanceCache;

        public ProductAssignService(IMapboxDistanceCacheService distanceCache)
        {
            _distanceCache = distanceCache;
        }

        public async Task<AssignProductResult> AssignProductsAsync(List<Guid> productIds, DateOnly workDate)
        {
            var result = new AssignProductResult();

            var config = FakeDataSeeder.CompanyConfigs;

            if (!config.Any())
                throw new Exception("Chưa có cấu hình company.");

            var sortedConfig = config.OrderBy(c => c.CompanyId).ToList();

            double totalPercent = sortedConfig.Sum(c => c.RatioPercent);
            if (Math.Abs(totalPercent - 100) > 0.1)
                throw new Exception($"Tổng tỉ lệ phần trăm hiện tại là {totalPercent}%, cần phải là 100%");

            double currentPivot = 0.0;
            foreach (var company in sortedConfig)
            {
                company.MinRange = currentPivot;
                currentPivot += (company.RatioPercent / 100.0);
                company.MaxRange = currentPivot;
            }

            var products = FakeDataSeeder.products
                .Where(p => productIds.Contains(p.Id))
                .ToList();

            if (!products.Any())
                throw new Exception("Không có product hợp lệ.");

            foreach (var product in products)
            {
                var post = FakeDataSeeder.posts.First(p => p.ProductId == product.Id);
                var userAddress = FakeDataSeeder.userAddress.First(a => a.UserId == post.SenderId);

                double magicNumber = GetStableHashRatio(product.Id);

                var targetCompany = sortedConfig.FirstOrDefault(t => magicNumber >= t.MinRange && magicNumber < t.MaxRange);

                if (targetCompany == null) targetCompany = sortedConfig.Last();

                ProductAssignCandidate? chosenCandidate = null;

                var targetCandidate = await FindBestSmallPointForCompanyAsync(targetCompany, userAddress);

                if (targetCandidate != null)
                {
                    chosenCandidate = targetCandidate;
                }
                else
                {
                    double bestDistance = double.MaxValue;

                    foreach (var otherCompany in sortedConfig.Where(c => c.CompanyId != targetCompany.CompanyId))
                    {
                        var candidate = await FindBestSmallPointForCompanyAsync(otherCompany, userAddress);
                        if (candidate != null && candidate.RoadKm < bestDistance)
                        {
                            bestDistance = candidate.RoadKm;
                            chosenCandidate = candidate;
                        }
                    }
                }

                if (chosenCandidate == null)
                {
                    result.TotalUnassigned++;
                    result.Details.Add(new { productId = product.Id, status = "no_candidate_available" });
                }
                else
                {
                    post.CollectionCompanyId = chosenCandidate.CompanyId;
                    post.AssignedSmallPointId = chosenCandidate.SmallPointId;
                    post.DistanceToPointKm = chosenCandidate.RoadKm;

                    result.TotalAssigned++;

                    string note = (chosenCandidate.CompanyId == targetCompany.CompanyId)
                        ? "Target Match"
                        : "Fallback Geo";

                    result.Details.Add(new
                    {
                        productId = product.Id,
                        companyId = chosenCandidate.CompanyId,
                        smallPointId = chosenCandidate.SmallPointId,
                        roadKm = $"{Math.Round(chosenCandidate.RoadKm, 2):0.00} km",
                        radiusKm = $"{Math.Round(chosenCandidate.HaversineKm, 2):0.00} km",
                        status = "assigned",
                        note = note
                    });
                }
            }

            return result;
        }

        private async Task<ProductAssignCandidate?> FindBestSmallPointForCompanyAsync(CompanyConfigItem company, UserAddress address)
        {
            ProductAssignCandidate? best = null;
            double minRoadKm = double.MaxValue;

            foreach (var sp in company.SmallPoints.Where(s => s.Active))
            {
                var small = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == sp.SmallPointId);
                if (small == null) continue;

                double hvDistance = GeoHelper.DistanceKm(small.Latitude, small.Longitude, address.Iat.Value, address.Ing.Value);
                if (hvDistance > sp.RadiusKm) continue;

                double roadKm = await _distanceCache.GetRoadDistanceKm(small.Latitude, small.Longitude, address.Iat.Value, address.Ing.Value);
                if (roadKm > sp.MaxRoadDistanceKm) continue;

                if (roadKm < minRoadKm)
                {
                    minRoadKm = roadKm;
                    best = new ProductAssignCandidate
                    {
                        ProductId = Guid.Empty,
                        CompanyId = company.CompanyId,
                        SmallPointId = sp.SmallPointId,
                        RoadKm = roadKm,
                        HaversineKm = hvDistance
                    };
                }
            }
            return best;
        }

        private double GetStableHashRatio(Guid id)
        {
            int hash = id.GetHashCode();
            if (hash < 0) hash = -hash;
            return (hash % 10000) / 10000.0;
        }

        public async Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate)
        {
            var result = new List<ProductByDateModel>();

            foreach (var post in FakeDataSeeder.posts)
            {
                if (!TryParseScheduleInfo(post.ScheduleJson!, out var dates))
                    continue;

                if (!dates.Contains(workDate))
                    continue;

                var product = FakeDataSeeder.products.First(pr => pr.Id == post.ProductId);
                if (product.Status != "Chờ gom nhóm") continue;

                var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);
                var address = FakeDataSeeder.userAddress
                    .FirstOrDefault(a => a.UserId == user.UserId)?.Address;

                var category = FakeDataSeeder.categories.FirstOrDefault(c => c.Id == product.CategoryId);
                var brand = FakeDataSeeder.brands.FirstOrDefault(b => b.BrandId == product.BrandId);

                result.Add(new ProductByDateModel
                {
                    ProductId = product.Id,
                    PostId = post.Id,
                    CategoryName = category?.Name ?? "Unknown Category", 
                    BrandName = brand?.Name ?? "Unknown Brand",      
                    UserName = user.Name,
                    Address = address
                });
            }

            return await Task.FromResult(result);
        }


        private bool TryParseScheduleInfo(string raw, out List<DateOnly> dates)
        {
            dates = new List<DateOnly>();
            if (string.IsNullOrWhiteSpace(raw)) return false;

            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = JsonSerializer.Deserialize<List<ScheduleDayDto>>(raw, opts);

                if (days == null) return false;

                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date))
                        dates.Add(date);
                }

                return dates.Any();
            }
            catch
            {
                return false;
            }
        }

        private class ScheduleDayDto
        {
            public string? PickUpDate { get; set; }
            public ScheduleSlotDto? Slots { get; set; }
        }

        private class ScheduleSlotDto
        {
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
        }
    }
}

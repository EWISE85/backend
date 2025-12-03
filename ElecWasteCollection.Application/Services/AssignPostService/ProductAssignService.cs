using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductAssignService : IProductAssignService
    {
        private readonly IMapboxDistanceCacheService _distanceCache;

        public ProductAssignService(IMapboxDistanceCacheService distanceCache)
        {
            _distanceCache = distanceCache;
        }

        public async Task<AssignProductResult> AssignProductsAsync( List<Guid> productIds, DateOnly workDate)
        {
            var result = new AssignProductResult();
            var config = FakeDataSeeder.CompanyConfigs;

            if (!config.Any())
                throw new Exception("Chưa có cấu hình company.");

            var products = FakeDataSeeder.products
                .Where(p => productIds.Contains(p.Id))
                .ToList();

            if (!products.Any())
                throw new Exception("Không có product hợp lệ.");

            int total = products.Count;
            int assignedCount = 0;

            foreach (var team in config)
            {
                int baseQuota = (int)(total * team.RatioPercent / 100.0);

                team.Quota = baseQuota;
                assignedCount += baseQuota;
            }

            int remainder = total - assignedCount;

            if (remainder > 0)
            {
                var sortedTeams = config.OrderByDescending(t => t.RatioPercent).ToList();

                int index = 0;
                while (remainder > 0)
                {
                    sortedTeams[index].Quota++;
                    remainder--;
                    index = (index + 1) % sortedTeams.Count;
                }
            }

            foreach (var product in products)
            {
                var post = FakeDataSeeder.posts.First(p => p.ProductId == product.Id);


                var userAddress = FakeDataSeeder.userAddress.First(a => a.UserId == post.SenderId);

                var candidates = new List<ProductAssignCandidate>();

                foreach (var team in config.Where(t => t.Quota > 0))
                {
                    foreach (var sp in team.SmallPoints.Where(s => s.Active))
                    {
                        var small = FakeDataSeeder.smallCollectionPoints
                            .First(p => p.Id == sp.SmallPointId);

                        double hvDistance = GeoHelper.DistanceKm(
                            small.Latitude, small.Longitude,
                            userAddress.Iat.Value, userAddress.Ing.Value);

                        if (hvDistance > sp.RadiusKm)
                            continue;

                        double roadKm = await _distanceCache.GetRoadDistanceKm(
                            small.Latitude, small.Longitude,
                            userAddress.Iat.Value, userAddress.Ing.Value);

                        if (roadKm > sp.MaxRoadDistanceKm)
                            continue;

                        candidates.Add(new ProductAssignCandidate
                        {
                            ProductId = product.Id,
                            CompanyId = team.CompanyId,
                            SmallPointId = sp.SmallPointId,
                            RoadKm = roadKm,
                            HaversineKm = hvDistance
                        });
                    }
                }

                if (!candidates.Any())
                {
                    result.TotalUnassigned++;
                    result.Details.Add(new { productId = product.Id, status = "no_candidate" });
                    continue;
                }

                var chosen = candidates.OrderBy(c => c.RoadKm).First();
                var chosenTeam = config.First(t => t.CompanyId == chosen.CompanyId);

                chosenTeam.Quota--;

                post.CollectionCompanyId = chosen.CompanyId;
                post.AssignedSmallPointId = chosen.SmallPointId;
                //
                post.DistanceToPointKm = chosen.RoadKm;

                string fRoad = $"{Math.Round(chosen.RoadKm, 2):0.00} km";
                string fRadius = $"{Math.Round(chosen.HaversineKm, 2):0.00} km";

                result.TotalAssigned++;

                result.Details.Add(new
                {
                    productId = product.Id,
                    companyId = chosen.CompanyId,
                    smallPointId = chosen.SmallPointId,
                    roadKm = fRoad,
                    radiusKm = fRadius,
                    status = "assigned"
                });
            }

            return result;
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

                result.Add(new ProductByDateModel
                {
                    ProductId = product.Id,
                    PostId = post.Id,                     
                    ProductName = post.Description?? product.Description,
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

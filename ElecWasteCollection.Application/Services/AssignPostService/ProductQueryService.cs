using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using System.Linq;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductQueryService : IProductQueryService
    {
        private readonly IMapboxDistanceCacheService _distance;

        private readonly Guid _attTrongLuong = FakeDataSeeder.ID_TrongLuong;
        private readonly Guid _attKhoiLuongGiat = FakeDataSeeder.ID_KhoiLuongGiat;

        private readonly Guid _attChieuDai = FakeDataSeeder.ID_ChieuDai;
        private readonly Guid _attChieuRong = FakeDataSeeder.ID_ChieuRong;
        private readonly Guid _attChieuCao = FakeDataSeeder.ID_ChieuCao;

        private readonly Guid _attDungTich = FakeDataSeeder.ID_DungTich;
        private readonly Guid _attKichThuocManHinh = FakeDataSeeder.ID_KichThuocManHinh;

        public ProductQueryService(IMapboxDistanceCacheService distance)
        {
            _distance = distance;
        }


        private (double weight, double volume) GetProductMetrics(Guid productId)
        {
            var pValues = FakeDataSeeder.productValues.Where(v => v.ProductId == productId).ToList();
            var allOptions = FakeDataSeeder.attributeOptions;

            double weight = 0;
            var weightAttributeIds = new[] { _attTrongLuong, _attKhoiLuongGiat, _attDungTich };

            foreach (var attId in weightAttributeIds)
            {
                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);
                if (pVal != null && pVal.AttributeOptionId.HasValue)
                {
                    var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                    if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight.Value > 0)
                    {
                        weight = opt.EstimateWeight.Value;
                        break;
                    }
                }
            }
            if (weight <= 0) weight = 1; 

            double length = pValues.FirstOrDefault(v => v.AttributeId == _attChieuDai)?.Value ?? 0;
            double width = pValues.FirstOrDefault(v => v.AttributeId == _attChieuRong)?.Value ?? 0;
            double height = pValues.FirstOrDefault(v => v.AttributeId == _attChieuCao)?.Value ?? 0;

            double volume = 0; 

            if (length > 0 && width > 0 && height > 0)
            {
                volume = length * width * height;
            }
            else
            {
                var volumeAttributeIds = new[] { _attKichThuocManHinh, _attDungTich, _attKhoiLuongGiat, _attTrongLuong };
                foreach (var attId in volumeAttributeIds)
                {
                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);
                    if (pVal != null && pVal.AttributeOptionId.HasValue)
                    {
                        var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateVolume.HasValue && opt.EstimateVolume.Value > 0)
                        {
                            volume = opt.EstimateVolume.Value * 1_000_000;
                            break;
                        }
                    }
                }
            }

            if (volume <= 0) volume = 1000; 

            return (weight, volume / 1_000_000.0);
        }

        public async Task<GetCompanyProductsResponse> GetCompanyProductsAsync(int companyId, DateOnly workDate)
        {
            var config = FakeDataSeeder.CompanyConfigs.FirstOrDefault(c => c.CompanyId == companyId)
                ?? throw new Exception("Company not found.");

            var posts = FakeDataSeeder.posts
                .Where(p => p.CollectionCompanyId == companyId)
                .Where(p =>
                {
                    if (!TryParseDates(p.ScheduleJson!, out var list)) return false;
                    return list.Contains(workDate);
                })
                .ToList();

            var products = posts.Select(p => FakeDataSeeder.products.First(x => x.Id == p.ProductId)).ToList();

            var response = new GetCompanyProductsResponse
            {
                CompanyId = companyId,
                CompanyName = FakeDataSeeder.collectionTeams.FirstOrDefault(t => t.Id == companyId)?.Name ?? $"Team {companyId}",
                WorkDate = workDate.ToString("yyyy-MM-dd"),
                TotalProducts = products.Count
            };

            double totalWeight = 0, totalVolume = 0;
            var grouped = posts.GroupBy(p => p.AssignedSmallPointId);

            foreach (var grp in grouped)
            {
                int spId = grp.Key ?? 0;
                var sp = FakeDataSeeder.smallCollectionPoints.First(s => s.Id == spId);
                var spConfig = config.SmallPoints.FirstOrDefault(s => s.SmallPointId == spId);

                var spDto = new SmallPointProductGroupDto
                {
                    SmallPointId = spId,
                    SmallPointName = sp.Name,
                    RadiusMaxConfigKm = spConfig?.RadiusKm ?? 0,
                    MaxRoadDistanceKm = spConfig?.MaxRoadDistanceKm ?? 0
                };

                foreach (var post in grp)
                {
                    var product = FakeDataSeeder.products.First(x => x.Id == post.ProductId);
                    var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);
                    var address = FakeDataSeeder.userAddress.First(a => a.UserId == user.UserId);

                    var metrics = GetProductMetrics(product.Id);

                    double radiusKm = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, address.Iat.Value, address.Ing.Value);
                    double roadKm = await _distance.GetRoadDistanceKm(sp.Latitude, sp.Longitude, address.Iat.Value, address.Ing.Value);

                    spDto.Products.Add(new ProductDetailDto
                    {
                        PostId = post.Id,
                        ProductId = product.Id,
                        SenderId = user.UserId,
                        UserName = user.Name,
                        Address = address.Address,
                        WeightKg = metrics.weight,
                        VolumeM3 = Math.Round(metrics.volume, 4), 
                        RadiusKm = $"{Math.Round(radiusKm, 2):0.00} km",
                        RoadKm = $"{Math.Round(roadKm, 2):0.00} km"
                    });

                    spDto.TotalWeightKg += metrics.weight;
                    spDto.TotalVolumeM3 += metrics.volume;
                }

                spDto.TotalVolumeM3 = Math.Round(spDto.TotalVolumeM3, 3);
                spDto.Total = spDto.Products.Count;

                totalWeight += spDto.TotalWeightKg;
                totalVolume += spDto.TotalVolumeM3;

                response.Points.Add(spDto);
            }

            response.TotalWeightKg = totalWeight;
            response.TotalVolumeM3 = Math.Round(totalVolume, 3);

            return response;
        }

        public async Task<SmallPointProductGroupDto> GetSmallPointProductsAsync(int smallPointId, DateOnly workDate)
        {
            var posts = FakeDataSeeder.posts
                .Where(p => p.AssignedSmallPointId == smallPointId)
                .Where(p =>
                {
                    if (!TryParseDates(p.ScheduleJson!, out var list)) return false;
                    return list.Contains(workDate);
                })
                .ToList();

            var sp = FakeDataSeeder.smallCollectionPoints.First(s => s.Id == smallPointId);

            var configItem = FakeDataSeeder.CompanyConfigs
                .SelectMany(c => c.SmallPoints)
                .FirstOrDefault(x => x.SmallPointId == smallPointId);

            var spDto = new SmallPointProductGroupDto
            {
                SmallPointId = smallPointId,
                SmallPointName = sp.Name,
                RadiusMaxConfigKm = configItem?.RadiusKm ?? 0,
                MaxRoadDistanceKm = configItem?.MaxRoadDistanceKm ?? 0
            };

            foreach (var post in posts)
            {
                var product = FakeDataSeeder.products.First(p => p.Id == post.ProductId);
                var user = FakeDataSeeder.users.First(u => u.UserId == post.SenderId);
                var address = FakeDataSeeder.userAddress.First(a => a.UserId == user.UserId);

                var metrics = GetProductMetrics(product.Id);

                double radiusKm = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, address.Iat.Value, address.Ing.Value);
                double roadKm = await _distance.GetRoadDistanceKm(sp.Latitude, sp.Longitude, address.Iat.Value, address.Ing.Value);

                spDto.Products.Add(new ProductDetailDto
                {
                    PostId = post.Id,
                    ProductId = product.Id,
                    SenderId = post.SenderId,
                    UserName = user.Name,
                    Address = address.Address,
                    WeightKg = metrics.weight,
                    VolumeM3 = Math.Round(metrics.volume, 4),
                    RadiusKm = $"{Math.Round(radiusKm, 2):0.00} km",
                    RoadKm = $"{Math.Round(roadKm, 2):0.00} km"
                });

                spDto.TotalWeightKg += metrics.weight;
                spDto.TotalVolumeM3 += metrics.volume;
            }

            spDto.TotalVolumeM3 = Math.Round(spDto.TotalVolumeM3, 3);
            spDto.Total = spDto.Products.Count;

            return spDto;
        }

        private bool TryParseDates(string scheduleJson, out List<DateOnly> dates)
        {
            dates = new();
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var list = JsonSerializer.Deserialize<List<ScheduleDayDto>>(scheduleJson, opts);
                if (list == null) return false;
                foreach (var d in list) { if (DateOnly.TryParse(d.PickUpDate, out var dt)) dates.Add(dt); }
                return dates.Any();
            }
            catch { return false; }
        }
        private class ScheduleDayDto { public string? PickUpDate { get; set; } public ScheduleSlotDto? Slots { get; set; } }
        private class ScheduleSlotDto { public string? StartTime { get; set; } public string? EndTime { get; set; } }

        public async Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync()
        {
            await Task.Yield();

            return FakeDataSeeder.CompanyConfigs.Select(company =>
            {
                var realCompany = FakeDataSeeder.collectionTeams.FirstOrDefault(t => t.Id == company.CompanyId);
                string companyName = realCompany?.Name ?? $"Company {company.CompanyId}";

                var smallPoints = company.SmallPoints.Select(cfgSp =>
                {
                    var realSP = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == cfgSp.SmallPointId);

                    return new SmallPointDto
                    {
                        SmallPointId = cfgSp.SmallPointId,
                        Name = realSP?.Name,
                        Lat = realSP?.Latitude ?? 0,
                        Lng = realSP?.Longitude ?? 0,
                        RadiusKm = cfgSp.RadiusKm,
                        MaxRoadDistanceKm = cfgSp.MaxRoadDistanceKm,
                        Active = cfgSp.Active
                    };
                }).ToList();

                return new CompanyWithPointsResponse
                {
                    CompanyId = company.CompanyId,
                    CompanyName = companyName,
                    SmallPoints = smallPoints
                };
            }).ToList();
        }

        public async Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(int companyId)
        {
            await Task.Yield();

            var company = FakeDataSeeder.CompanyConfigs.FirstOrDefault(c => c.CompanyId == companyId)
                ?? throw new Exception("Company not found.");

            return company.SmallPoints.Select(cfgSp =>
            {
                var realSP = FakeDataSeeder.smallCollectionPoints.FirstOrDefault(p => p.Id == cfgSp.SmallPointId);

                return new SmallPointDto
                {
                    SmallPointId = cfgSp.SmallPointId,
                    Name = realSP?.Name,
                    Lat = realSP?.Latitude ?? 0,
                    Lng = realSP?.Longitude ?? 0,
                    RadiusKm = cfgSp.RadiusKm,
                    MaxRoadDistanceKm = cfgSp.MaxRoadDistanceKm,
                    Active = cfgSp.Active
                };
            }).ToList();
        }
        public async Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(int companyId)
        {
            await Task.Yield();

            var company = FakeDataSeeder.CompanyConfigs
                .FirstOrDefault(c => c.CompanyId == companyId)
                ?? throw new Exception("Company not found.");

            var realCompany = FakeDataSeeder.collectionTeams
                .FirstOrDefault(t => t.Id == companyId);

            string companyName = realCompany?.Name ?? $"Company {companyId}";

            var smallPoints = company.SmallPoints.Select(cfgSp =>
            {
                var realSP = FakeDataSeeder.smallCollectionPoints
                    .FirstOrDefault(p => p.Id == cfgSp.SmallPointId);

                return new SmallPointDto
                {
                    SmallPointId = cfgSp.SmallPointId,
                    Name = realSP?.Name,
                    Lat = realSP?.Latitude ?? 0,
                    Lng = realSP?.Longitude ?? 0,
                    RadiusKm = cfgSp.RadiusKm,
                    MaxRoadDistanceKm = cfgSp.MaxRoadDistanceKm,
                    Active = cfgSp.Active
                };
            }).ToList();

            return new CompanyConfigDto
            {
                CompanyId = company.CompanyId,
                CompanyName = companyName,
                RatioPercent = company.RatioPercent,
                SmallPoints = smallPoints
            };
        }

    }
}
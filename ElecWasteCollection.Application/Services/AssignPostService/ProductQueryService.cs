using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductQueryService : IProductQueryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapboxDistanceCacheService _distance;

        // Xử lý sau 
        private const string NAME_TRONG_LUONG = "Trọng lượng";
        private const string NAME_KHOI_LUONG_GIAT = "Khối lượng giặt";
        private const string NAME_CHIEU_DAI = "Chiều dài";
        private const string NAME_CHIEU_RONG = "Chiều rộng";
        private const string NAME_CHIEU_CAO = "Chiều cao";
        private const string NAME_DUNG_TICH = "Dung tích";
        private const string NAME_KICH_THUOC_MAN = "Kích thước màn hình";

        public ProductQueryService(IUnitOfWork unitOfWork, IMapboxDistanceCacheService distance)
        {
            _unitOfWork = unitOfWork;
            _distance = distance;
        }

        private async Task<Dictionary<string, Guid>> GetAttributeIdMapAsync()
        {
            var targetNames = new[]
            {
                NAME_TRONG_LUONG, NAME_KHOI_LUONG_GIAT,
                NAME_CHIEU_DAI, NAME_CHIEU_RONG, NAME_CHIEU_CAO,
                NAME_DUNG_TICH, NAME_KICH_THUOC_MAN
            };

            var atts = await _unitOfWork.Attributes.GetAllAsync(filter: a => targetNames.Contains(a.Name));

            return atts.ToDictionary(k => k.Name, v => v.AttributeId);
        }

        private async Task<(double weight, double volume)> GetProductMetricsAsync(Guid productId, Dictionary<string, Guid> attMap)
        {
            var pValues = await _unitOfWork.ProductValues.GetAllAsync(filter: v => v.ProductId == productId);

            var optionIds = pValues.Where(v => v.AttributeOptionId.HasValue).Select(v => v.AttributeOptionId.Value).ToList();
            var relatedOptions = await _unitOfWork.AttributeOptions.GetAllAsync(filter: o => optionIds.Contains(o.OptionId));

            double weight = 0;

            var weightKeys = new[] { NAME_TRONG_LUONG, NAME_KHOI_LUONG_GIAT, NAME_DUNG_TICH };

            foreach (var key in weightKeys)
            {
                if (!attMap.ContainsKey(key)) continue;
                var attId = attMap[key];

                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);
                if (pVal != null && pVal.AttributeOptionId.HasValue)
                {
                    var opt = relatedOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                    if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight.Value > 0)
                    {
                        weight = opt.EstimateWeight.Value;
                        break;
                    }
                }
            }
            if (weight <= 0) weight = 1;

            double volume = 0;

            double GetAttributeValue(string attrName)
            {
                if (attMap.ContainsKey(attrName))
                {
                    return pValues.FirstOrDefault(v => v.AttributeId == attMap[attrName])?.Value ?? 0;
                }
                return 0;
            }

            double length = GetAttributeValue(NAME_CHIEU_DAI);
            double width = GetAttributeValue(NAME_CHIEU_RONG);
            double height = GetAttributeValue(NAME_CHIEU_CAO);

            if (length > 0 && width > 0 && height > 0)
            {
                volume = length * width * height;
            }
            else
            {
                var volumeKeys = new[] { NAME_KICH_THUOC_MAN, NAME_DUNG_TICH, NAME_KHOI_LUONG_GIAT, NAME_TRONG_LUONG };
                foreach (var key in volumeKeys)
                {
                    if (!attMap.ContainsKey(key)) continue;
                    var attId = attMap[key];

                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);
                    if (pVal != null && pVal.AttributeOptionId.HasValue)
                    {
                        var opt = relatedOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
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

        public async Task<GetCompanyProductsResponse> GetCompanyProductsAsync(string companyId, DateOnly workDate)
        {
            var attMap = await GetAttributeIdMapAsync();

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            var companyEntity = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "SmallCollectionPoints");

            if (companyEntity == null) throw new Exception("Company not found.");

            var allPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.CollectionCompanyId == companyId,
                includeProperties: "Product,Product.Category,Product.Brand,Sender"
            );

            var posts = allPosts.Where(p =>
            {
                if (!TryParseDates(p.ScheduleJson!, out var list)) return false;
                return list.Contains(workDate);
            }).ToList();

            var response = new GetCompanyProductsResponse
            {
                CompanyId = companyId,
                CompanyName = companyEntity.Name,
                WorkDate = workDate.ToString("yyyy-MM-dd"),
                TotalProducts = posts.Select(p => p.ProductId).Distinct().Count()
            };

            double totalWeight = 0, totalVolume = 0;
            var grouped = posts.GroupBy(p => p.AssignedSmallPointId);

            foreach (var grp in grouped)
            {
                var spId = grp.Key;
                var spEntity = companyEntity.SmallCollectionPoints.FirstOrDefault(s => s.SmallCollectionPointsId == spId);
                if (spEntity == null) continue;

                double radiusConfig = GetConfigValue(allConfigs, null, spId, SystemConfigKey.RADIUS_KM, 0);
                double maxRoadConfig = GetConfigValue(allConfigs, null, spId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0);

                var spDto = new SmallPointProductGroupDto
                {
                    SmallPointId = spId,
                    SmallPointName = spEntity.Name,
                    RadiusMaxConfigKm = radiusConfig,
                    MaxRoadDistanceKm = maxRoadConfig
                };

                foreach (var post in grp)
                {
                    var product = post.Product;
                    var user = post.Sender;
                    if (product == null || user == null) continue;

                    string displayAddress = !string.IsNullOrEmpty(post.Address) ? post.Address : "Unknown";

                    double lat = 0, lng = 0;
                    if (!string.IsNullOrEmpty(post.Address))
                    {
                        var addressEntity = await _unitOfWork.UserAddresses.GetAsync(
                            a => a.UserId == user.UserId && a.Address == post.Address
                        );
                        if (addressEntity != null && addressEntity.Iat.HasValue && addressEntity.Ing.HasValue)
                        {
                            lat = addressEntity.Iat.Value;
                            lng = addressEntity.Ing.Value;
                        }
                    }

                    var metrics = await GetProductMetricsAsync(product.ProductId, attMap);

                    double radiusKm = 0;
                    double roadKm = 0;

                    if (lat != 0 && lng != 0)
                    {
                        radiusKm = GeoHelper.DistanceKm(spEntity.Latitude, spEntity.Longitude, lat, lng);
                        roadKm = await _distance.GetRoadDistanceKm(spEntity.Latitude, spEntity.Longitude, lat, lng);
                    }

                    spDto.Products.Add(new ProductDetailDto
                    {
                        PostId = post.PostId,
                        ProductId = product.ProductId,
                        SenderId = user.UserId,
                        UserName = user.Name,
                        Address = displayAddress,
                        CategoryName = product.Category?.Name ?? "Unknown",
                        BrandName = product.Brand?.Name ?? "Unknown",
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

        public async Task<SmallPointProductGroupDto> GetSmallPointProductsAsync(string smallPointId, DateOnly workDate)
        {
            var attMap = await GetAttributeIdMapAsync();
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            var company = (await _unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints"))
                .FirstOrDefault(c => c.SmallCollectionPoints.Any(sp => sp.SmallCollectionPointsId == smallPointId));

            if (company == null) throw new Exception("Small Point not found.");
            var spEntity = company.SmallCollectionPoints.First(s => s.SmallCollectionPointsId == smallPointId);

            var allPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.AssignedSmallPointId == smallPointId
                && p.Product.Status == "Chờ gom nhóm",
                includeProperties: "Product,Product.Category,Product.Brand,Sender"
            );

            var posts = allPosts.Where(p =>
            {
                if (!TryParseDates(p.ScheduleJson!, out var list)) return false;
                return list.Contains(workDate);
            }).ToList();

            double radiusConfig = GetConfigValue(allConfigs, null, smallPointId, SystemConfigKey.RADIUS_KM, 0);
            double maxRoadConfig = GetConfigValue(allConfigs, null, smallPointId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0);

            var spDto = new SmallPointProductGroupDto
            {
                SmallPointId = smallPointId,
                SmallPointName = spEntity.Name,
                RadiusMaxConfigKm = radiusConfig,
                MaxRoadDistanceKm = maxRoadConfig
            };

            foreach (var post in posts)
            {
                var product = post.Product;
                var user = post.Sender;
                if (product == null || user == null) continue;

                string displayAddress = !string.IsNullOrEmpty(post.Address) ? post.Address : "Unknown";

                double lat = 0, lng = 0;
                if (!string.IsNullOrEmpty(post.Address))
                {
                    var addressEntity = await _unitOfWork.UserAddresses.GetAsync(
                        a => a.UserId == user.UserId && a.Address == post.Address
                    );
                    if (addressEntity != null && addressEntity.Iat.HasValue && addressEntity.Ing.HasValue)
                    {
                        lat = addressEntity.Iat.Value;
                        lng = addressEntity.Ing.Value;
                    }
                }

                var metrics = await GetProductMetricsAsync(product.ProductId, attMap);

                double radiusKm = 0;
                double roadKm = 0;

                if (lat != 0 && lng != 0)
                {
                    radiusKm = GeoHelper.DistanceKm(spEntity.Latitude, spEntity.Longitude, lat, lng);
                    roadKm = await _distance.GetRoadDistanceKm(spEntity.Latitude, spEntity.Longitude, lat, lng);
                }

                spDto.Products.Add(new ProductDetailDto
                {
                    PostId = post.PostId,
                    ProductId = product.ProductId,
                    SenderId = user.UserId,
                    UserName = user.Name,
                    Address = displayAddress,
                    CategoryName = product.Category?.Name ?? "Unknown",
                    BrandName = product.Brand?.Name ?? "Unknown",
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

        public async Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == "CollectionCompany",
                includeProperties: "SmallCollectionPoints");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return companies.Select(company => new CompanyWithPointsResponse
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                SmallPoints = company.SmallCollectionPoints.Select(sp => new SmallPointDto
                {
                    SmallPointId = sp.SmallCollectionPointsId,
                    Name = sp.Name,
                    Lat = sp.Latitude,
                    Lng = sp.Longitude,
                    RadiusKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 0),
                    MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                    Active = true
                }).ToList()
            }).ToList();
        }

        public async Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "SmallCollectionPoints");

            if (company == null) throw new Exception("Company not found.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return company.SmallCollectionPoints.Select(sp => new SmallPointDto
            {
                SmallPointId = sp.SmallCollectionPointsId,
                Name = sp.Name,
                Lat = sp.Latitude,
                Lng = sp.Longitude,
                RadiusKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 0),
                MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                Active = true
            }).ToList();
        }

        public async Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "SmallCollectionPoints");

            if (company == null) throw new Exception("Company not found.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return new CompanyConfigDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                RatioPercent = GetConfigValue(allConfigs, company.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0),
                SmallPoints = company.SmallCollectionPoints.Select(sp => new SmallPointDto
                {
                    SmallPointId = sp.SmallCollectionPointsId,
                    Name = sp.Name,
                    Lat = sp.Latitude,
                    Lng = sp.Longitude,
                    RadiusKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 0),
                    MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                    Active = true
                }).ToList()
            };
        }


        private bool TryParseDates(string scheduleJson, out List<DateOnly> dates)
        {
            dates = new();
            if (string.IsNullOrWhiteSpace(scheduleJson)) return false;
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

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == pointId);

            if (config == null && companyId != null && pointId != null)
            {
            }

            if (config != null && double.TryParse(config.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private class ScheduleDayDto { public string? PickUpDate { get; set; } }
    }
}
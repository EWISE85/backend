using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model;
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
        private readonly IProductQueryRepository _productQueryRepository;

        private const string NAME_TRONG_LUONG = "Trọng lượng";
        private const string NAME_KHOI_LUONG_GIAT = "Khối lượng giặt";
        private const string NAME_CHIEU_DAI = "Chiều dài";
        private const string NAME_CHIEU_RONG = "Chiều rộng";
        private const string NAME_CHIEU_CAO = "Chiều cao";
        private const string NAME_DUNG_TICH = "Dung tích";
        private const string NAME_KICH_THUOC_MAN = "Kích thước màn hình";

        public ProductQueryService(IUnitOfWork unitOfWork, IMapboxDistanceCacheService distance, IProductQueryRepository productQueryRepository)
        {
            _unitOfWork = unitOfWork;
            _distance = distance;
            _productQueryRepository = productQueryRepository;
        }

        private async Task<Dictionary<string, Guid>> GetAttributeIdMapAsync()
        {
            var targetKeywords = new[]
            {
                NAME_TRONG_LUONG, NAME_KHOI_LUONG_GIAT,
                NAME_CHIEU_DAI, NAME_CHIEU_RONG, NAME_CHIEU_CAO,
                NAME_DUNG_TICH, NAME_KICH_THUOC_MAN
            };

            var allAttributes = await _unitOfWork.Attributes.GetAllAsync();
            var map = new Dictionary<string, Guid>();

            foreach (var key in targetKeywords)
            {
                var match = allAttributes.FirstOrDefault(a => a.Name.Contains(key, StringComparison.OrdinalIgnoreCase));
                if (match != null && !map.ContainsKey(key))
                {
                    map.Add(key, match.AttributeId);
                }
            }
            return map;
        }

        private async Task<(double weight, double volume, double length, double width, double height)> GetProductMetricsAsync(Guid productId, Dictionary<string, Guid> attMap)
        {
            var pValues = await _unitOfWork.ProductValues.GetAllAsync(filter: v => v.ProductId == productId);

            var optionIds = pValues.Where(v => v.AttributeOptionId.HasValue).Select(v => v.AttributeOptionId.Value).ToList();

            var relatedOptions = optionIds.Any()
                ? (await _unitOfWork.AttributeOptions.GetAllAsync(filter: o => optionIds.Contains(o.OptionId))).ToList()
                : new List<AttributeOptions>();

            double weight = 0;
            var weightKeys = new[] { NAME_TRONG_LUONG, NAME_KHOI_LUONG_GIAT, NAME_DUNG_TICH };

            foreach (var key in weightKeys)
            {
                if (!attMap.ContainsKey(key)) continue;
                var attId = attMap[key];
                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attId);

                if (pVal != null)
                {
                    if (pVal.AttributeOptionId.HasValue)
                    {
                        var opt = relatedOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight.Value > 0)
                        {
                            weight = opt.EstimateWeight.Value;
                            break;
                        }
                    }
                    if (pVal.Value.HasValue && pVal.Value.Value > 0)
                    {
                        weight = pVal.Value.Value;
                        break;
                    }
                }
            }
            if (weight <= 0) weight = 1;

            double GetAttributeValue(string attrName)
            {
                if (attMap.ContainsKey(attrName))
                {
                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == attMap[attrName]);
                    return pVal?.Value ?? 0;
                }
                return 0;
            }

            double length = GetAttributeValue(NAME_CHIEU_DAI);
            double width = GetAttributeValue(NAME_CHIEU_RONG);
            double height = GetAttributeValue(NAME_CHIEU_CAO);
            double volume = 0;

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

            return (weight, volume / 1_000_000.0, length, width, height);
        }

        public async Task<GetCompanyProductsResponse> GetCompanyProductsAsync(string companyId, DateOnly workDate)
        {
            var attMap = await GetAttributeIdMapAsync();
            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();
            var companyEntity = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "CollectionUnits");

            if (companyEntity == null) throw new Exception("Không tìm thấy công ty nào.");

            var allPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.CompanyId == companyId,
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
                TotalProducts = posts
                .Where(p => p.Product != null)
                .Select(p => p.PostId)
                .Distinct()
                .Count()
            };

            double totalWeight = 0, totalVolume = 0;
            var grouped = posts.GroupBy(p => p.AssignedCollectionUnitId);

            foreach (var grp in grouped)
            {
                var spId = grp.Key;
                var spEntity = companyEntity.CollectionUnits.FirstOrDefault(s => s.CollectionUnitId == spId);
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

                    string displayAddress = !string.IsNullOrEmpty(post.Address) ? post.Address : "Không có";

                    double radiusKm = 0;
                    double roadKm = post.DistanceToPointKm ?? 0;

                    if (!string.IsNullOrEmpty(post.Address))
                    {
                        var addressEntity = await _unitOfWork.UserAddresses.GetAsync(
                            a => a.UserId == user.UserId && a.Address == post.Address
                        );

                        if (addressEntity != null && addressEntity.Iat.HasValue && addressEntity.Ing.HasValue)
                        {
                            radiusKm = GeoHelper.DistanceKm(
                                spEntity.Latitude,
                                spEntity.Longitude,
                                addressEntity.Iat.Value,
                                addressEntity.Ing.Value
                            );
                        }
                    }

                    var metrics = await GetProductMetricsAsync(product.ProductId, attMap);
                    string dimensionStr = (metrics.length > 0 && metrics.width > 0 && metrics.height > 0)
                        ? $"{metrics.length} x {metrics.width} x {metrics.height}"
                        : "Chưa cập nhật";

                    spDto.Products.Add(new ProductDetailDto
                    {
                        ProductId = product.ProductId,
                        SenderId = user.UserId,
                        UserName = user.Name,
                        Address = displayAddress,
                        CategoryName = product.Category?.Name ?? "Không rõ",
                        BrandName = product.Brand?.Name ?? "Không rõ",
                        WeightKg = metrics.weight,
                        VolumeM3 = Math.Round(metrics.volume, 4),
                        Length = metrics.length,
                        Width = metrics.width,
                        Height = metrics.height,
                        Dimensions = dimensionStr,
                        RadiusKm = (double)Math.Round(radiusKm, 2),
                        RoadKm = (double)Math.Round((post.DistanceToPointKm ?? 0), 2)
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
            
            response.TotalWeightKg = Math.Round(totalWeight, 2);
            response.TotalVolumeM3 = Math.Round(totalVolume, 3);
            return response;
        }

        public async Task<PagedSmallPointProductGroupDto> GetSmallPointProductsPagedAsync(string smallPointId, DateOnly workDate, int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var attMap = await GetAttributeIdMapAsync();

            var sp = await _unitOfWork.CollectionUnits
                .GetAsync(s => s.CollectionUnitId == smallPointId);

            if (sp == null) throw new Exception("Không tìm thấy trạm.");
            var allPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.Product != null
                          && p.Product.CollectionUnitId == smallPointId
                          && p.Product.AssignedAt <= workDate
                          && p.Product.Status == ProductStatus.CHO_GOM_NHOM.ToString(),
                includeProperties: "Product,Product.Category,Product.Brand,Sender,Product.User"
            );

            //var filteredPosts = allPosts.ToList();

            var filteredPosts = allPosts.Where(p =>
            {
                if (TryParseScheduleInfo(p.ScheduleJson, out var info)) 
                {
                    return info.SpecificDates.Contains(workDate);
                }
                return false;
            }).ToList();

            int totalCount = filteredPosts.Count;
            var pagedPosts = filteredPosts
                                 .Skip((page - 1) * limit)
                                 .Take(limit)
                                 .ToList();

            var result = new PagedSmallPointProductGroupDto
            {
                SmallPointId = smallPointId,
                SmallPointName = sp.Name,
                Page = page,
                Limit = limit,
                TotalItems = totalCount,
                Products = new List<ProductDetailDto>(),
                TotalWeightKg = 0,
                TotalVolumeM3 = 0
            };

            foreach (var post in pagedPosts)
            {
                var product = post.Product!;
                var metrics = await GetProductMetricsAsync(product.ProductId, attMap);
                var rawPhone = product.User?.Phone ?? post.Sender?.Phone ?? "N/A";

                result.Products.Add(new ProductDetailDto
                {
                    ProductId = product.ProductId,
                    SenderId = product.UserId,
                    UserName = product.User?.Name ?? post.Sender?.Name ?? "N/A",
                    Address = post.Address ?? "N/A",
                    PhoneNumber = rawPhone,
                    CategoryName = product.Category?.Name ?? "",
                    BrandName = product.Brand?.Name ?? "",
                    WeightKg = metrics.weight,
                    VolumeM3 = Math.Round(metrics.volume, 4),
                    Length = metrics.length,
                    Width = metrics.width,
                    Height = metrics.height,
                    Dimensions = $"{metrics.length} x {metrics.width} x {metrics.height}"
                });

                result.TotalWeightKg += metrics.weight;
                result.TotalVolumeM3 += metrics.volume;
            }

            result.TotalWeightKg = Math.Round(result.TotalWeightKg, 2);
            result.TotalVolumeM3 = Math.Round(result.TotalVolumeM3, 3);

            return result;
        }

        public async Task<List<CompanyWithPointsResponse>> GetCompaniesWithSmallPointsAsync()
        {
            var companies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString(),
                includeProperties: "CollectionUnits");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return companies.Select(company => new CompanyWithPointsResponse
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                SmallPoints = company.CollectionUnits.Select(sp => new SmallPointDto
                {
                    SmallPointId = sp.CollectionUnitId,
                    Name = sp.Name,
                    Lat = sp.Latitude,
                    Lng = sp.Longitude,
                    RadiusKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.RADIUS_KM, 0),
                    MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                    Active = true
                }).ToList()
            }).ToList();
        }

        public async Task<List<SmallPointDto>> GetSmallPointsByCompanyIdAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "CollectionUnits");

            if (company == null) throw new Exception("Không tìm thấy trạm thu gom nào.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return company.CollectionUnits.Select(sp => new SmallPointDto
            {
                SmallPointId = sp.CollectionUnitId,
                Name = sp.Name,
                Lat = sp.Latitude,
                Lng = sp.Longitude,
                RadiusKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.RADIUS_KM, 0),
                MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                Active = true
            }).ToList();
        }

        public async Task<CompanyConfigDto> GetCompanyConfigByCompanyIdAsync(string companyId)
        {
            var company = await _unitOfWork.Companies.GetAsync(
                filter: c => c.CompanyId == companyId,
                includeProperties: "CollectionUnits");

            if (company == null) throw new Exception("Không tìm thấy trạm thu gom nào.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            return new CompanyConfigDto
            {
                CompanyId = company.CompanyId,
                CompanyName = company.Name,
                RatioPercent = GetConfigValue(allConfigs, company.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0),
                SmallPoints = company.CollectionUnits.Select(sp => new SmallPointDto
                {
                    SmallPointId = sp.CollectionUnitId,
                    Name = sp.Name,
                    Lat = sp.Latitude,
                    Lng = sp.Longitude,
                    RadiusKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.RADIUS_KM, 0),
                    MaxRoadDistanceKm = GetConfigValue(allConfigs, null, sp.CollectionUnitId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 0),
                    Active = true
                }).ToList()
            };
        }

        public async Task<object> GetProductIdsAtSmallPointAsync(string smallPointId, DateOnly workDate)
        {
            var posts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.AssignedCollectionUnitId == smallPointId
                          && p.Product != null
                          && p.Product.Status == ProductStatus.CHO_GOM_NHOM.ToString(),
                includeProperties: "Product"
            );

            var listIds = new List<string>();

            // 2. Lọc theo ngày làm việc (xử lý JSON Schedule)
            foreach (var post in posts)
            {
                // Tái sử dụng hàm TryParseDates có sẵn trong class
                if (!TryParseDates(post.ScheduleJson!, out var dates))
                    continue;

                if (dates.Contains(workDate))
                {
                    listIds.Add(post.Product.ProductId.ToString());
                }
            }

            // 3. Trả về object kết quả
            return new
            {
                Total = listIds.Count,
                List = listIds
            };
        }
        public async Task<List<CompanyDailySummaryDto>> GetCompanySummariesByDateAsync(DateOnly workDate)
        {
            var allPosts = await _unitOfWork.Posts.GetAllAsync();

            var activePosts = allPosts.Where(p =>
            {
                if (string.IsNullOrEmpty(p.ScheduleJson) || !TryParseDates(p.ScheduleJson, out var list))
                    return false;
                return list.Contains(workDate);
            }).ToList();

            if (!activePosts.Any()) return new List<CompanyDailySummaryDto>();

            var companyIds = activePosts.Select(p => p.CompanyId).Distinct().ToList();

            var companies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => companyIds.Contains(c.CompanyId),
                includeProperties: "CollectionUnits"
            );

            var response = new List<CompanyDailySummaryDto>();

            foreach (var company in companies)
            {
                var companyPosts = activePosts.Where(p => p.CompanyId == company.CompanyId).ToList();

                var companyDto = new CompanyDailySummaryDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.Name,
                    TotalCompanyProducts = companyPosts.Select(p => p.PostId).Distinct().Count()
                };

                var pointGroups = companyPosts.GroupBy(p => p.AssignedCollectionUnitId);

                foreach (var grp in pointGroups)
                {
                    var spId = grp.Key;
                    var spEntity = company.CollectionUnits?
                        .FirstOrDefault(s => s.CollectionUnitId == spId);

                    var spName = spEntity != null ? spEntity.Name : "Điểm thu gom không xác định";

                    companyDto.Points.Add(new SmallPointSummaryDto
                    {
                        SmallCollectionId = spId,
                        Name = spName,
                        TotalProduct = grp.Select(p => p.PostId).Distinct().Count()
                    });
                }

                response.Add(companyDto);
            }

            return response;
        }

        public async Task<List<CompanyMetricsDto>> GetAllCompaniesDailyMetricsAsync(DateOnly workDate)
        {
            var targetType = CompanyType.CTY_TAI_CHE.ToString();
            var companies = await _unitOfWork.Companies.GetAllAsync(
                filter: c => c.CompanyType == targetType,
                includeProperties: "CollectionUnits"
            );  

            var attMap = await GetAttributeIdMapAsync();
            var allOptions = await _unitOfWork.AttributeOptions.GetAllAsync();

            var allAssignedProducts = await _unitOfWork.Products.GetAllAsync(
                filter: p => p.AssignedAt == workDate && p.CollectionUnitId != null,
                includeProperties: "ProductValues"
            );

            var result = new List<CompanyMetricsDto>();

            foreach (var company in companies)
            {
                var pointMetricsList = new List<SmallPointMetricsDto>();

                foreach (var point in company.CollectionUnits)
                {
                    var productsInPoint = allAssignedProducts
                        .Where(p => p.CollectionUnitId == point.CollectionUnitId)
                        .ToList();

                    double pointWeight = 0;
                    double pointVolume = 0;

                    foreach (var product in productsInPoint)
                    {
                        var m = CalculateMetricsInProcess(product.ProductValues.ToList(), attMap, allOptions.ToList());
                        pointWeight += m.weight;
                        pointVolume += m.volume;
                    }

                    pointMetricsList.Add(new SmallPointMetricsDto
                    {
                        PointId = point.CollectionUnitId,
                        PointName = point.Name,
                        TotalOrders = productsInPoint.Count,
                        TotalWeightKg = Math.Round(pointWeight, 2),
                        TotalVolumeM3 = Math.Round(pointVolume, 4)
                    });
                }

                result.Add(new CompanyMetricsDto
                {
                    CompanyId = company.CompanyId,
                    CompanyName = company.Name,
                    Date = workDate,
                    TotalOrders = pointMetricsList.Sum(x => x.TotalOrders),
                    TotalWeightKg = Math.Round(pointMetricsList.Sum(x => x.TotalWeightKg), 2),
                    TotalVolumeM3 = Math.Round(pointMetricsList.Sum(x => x.TotalVolumeM3), 4),
                    SmallCollectionPoints = pointMetricsList
                });
            }

            return result;
        }

        public async Task<SmallPointCollectionMetricsDto> GetSmallPointProductsPagedStatusAsync(string smallPointId, DateOnly workDate, int page, int limit)
        {
            if (page <= 0) page = 1;
            if (limit <= 0) limit = 10;

            var sp = await _unitOfWork.CollectionUnits.GetByIdAsync(smallPointId)
                      ?? throw new Exception("Không tìm thấy trạm thu gom.");

            var allPosts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.Product != null &&
                             p.Product.AssignedAt == workDate &&
                             p.Product.CollectionUnitId == smallPointId,
                includeProperties: "Product,Product.Category,Product.Brand,Sender,Product.User"
            );

            int totalCount = allPosts.Count();
            var allProductIds = allPosts
                .Where(p => p.Product != null)
                .Select(p => p.Product.ProductId)
                .ToList();
            var allProductValues = await _unitOfWork.ProductValues.GetAllAsync(pv => allProductIds.Contains(pv.ProductId));
            var allOptions = await _unitOfWork.AttributeOptions.GetAllAsync();
            var attMap = await GetAttributeIdMapAsync();

            var result = new SmallPointCollectionMetricsDto
            {
                SmallPointId = smallPointId,
                SmallPointName = sp.Name,
                Page = page,
                Limit = limit,
                TotalItems = totalCount,
                Products = new List<PointProductMetricDetailDto>()
            };

            foreach (var post in allPosts)
            {
                var product = post.Product!;
                var currentPValues = allProductValues.Where(pv => pv.ProductId == product.ProductId).ToList();
                var m = CalculateMetricsInProcess(currentPValues, attMap, allOptions.ToList());

                result.TotalWeightKg += m.weight;
                result.TotalVolumeM3 += m.volume;
            }

            var pagedPosts = allPosts
                            .Skip((page - 1) * limit)
                            .Take(limit)
                            .ToList();

            foreach (var post in pagedPosts)
            {
                var product = post.Product!;
                var currentPValues = allProductValues.Where(pv => pv.ProductId == product.ProductId).ToList();
                var m = CalculateMetricsInProcess(currentPValues, attMap, allOptions.ToList());

                result.Products.Add(new PointProductMetricDetailDto
                {
                    ProductId = product.ProductId,
                    SenderId = product.UserId,
                    UserName = product.User?.Name ?? post.Sender?.Name ?? "N/A",
                    Address = post.Address ?? "N/A",
                    CategoryName = product.Category?.Name ?? "N/A",
                    BrandName = product.Brand?.Name ?? "N/A",
                    WeightKg = Math.Round(m.weight, 2),
                    VolumeM3 = Math.Round(m.volume, 5),
                    Length = m.length,
                    Width = m.width,
                    Height = m.height,
                    Dimensions = $"{m.length} x {m.width} x {m.height}"
                });
            }

            result.TotalWeightKg = Math.Round(result.TotalWeightKg, 2);
            result.TotalVolumeM3 = Math.Round(result.TotalVolumeM3, 3);

            return result;
        }

        private (double weight, double volume, double length, double width, double height) CalculateMetricsInProcess(
    List<ProductValues> pValues,
    Dictionary<string, Guid> attMap,
    List<AttributeOptions> allOptions)
        {
            // 1. Tính toán Trọng lượng
            double weight = 0;
            var weightKeys = new[] { "Trọng lượng", "Khối lượng giặt", "Dung tích" };

            foreach (var key in weightKeys)
            {
                if (!attMap.TryGetValue(key, out var attrId)) continue;

                var pVal = pValues.FirstOrDefault(v => v.AttributeId == attrId);
                if (pVal != null)
                {
                    if (pVal.AttributeOptionId.HasValue)
                    {
                        var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateWeight.HasValue && opt.EstimateWeight > 0)
                        {
                            weight = opt.EstimateWeight.Value;
                            break;
                        }
                    }
                    if (pVal.Value.HasValue && pVal.Value.Value > 0)
                    {
                        weight = pVal.Value.Value;
                        break;
                    }
                }
            }
            if (weight <= 0) weight = 3; 

            double GetVal(string k)
            {
                if (!attMap.TryGetValue(k, out var id)) return 0;
                return pValues.FirstOrDefault(v => v.AttributeId == id)?.Value ?? 0;
            }

            double length = GetVal("Chiều dài");
            double width = GetVal("Chiều rộng");
            double height = GetVal("Chiều cao");

            double volume = 0;
            if (length > 0 && width > 0 && height > 0)
            {
                volume = length * width * height;
            }
            else
            {
                var volKeys = new[] { "Kích thước màn hình", "Dung tích", "Khối lượng giặt", "Trọng lượng" };
                foreach (var key in volKeys)
                {
                    if (!attMap.TryGetValue(key, out var id)) continue;

                    var pVal = pValues.FirstOrDefault(v => v.AttributeId == id);
                    if (pVal != null && pVal.AttributeOptionId.HasValue)
                    {
                        var opt = allOptions.FirstOrDefault(o => o.OptionId == pVal.AttributeOptionId);
                        if (opt != null && opt.EstimateVolume.HasValue && opt.EstimateVolume > 0)
                        {
                            volume = opt.EstimateVolume.Value * 1_000_000;
                            break;
                        }
                    }
                }
            }
            if (volume <= 0) volume = 1000; 

            return (weight, volume / 1_000_000.0, length, width, height);
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
                x.CollectionUnitId == pointId);

            if (config != null && double.TryParse(config.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private class ScheduleDayDto { public string? PickUpDate { get; set; } }
        private static bool TryParseScheduleInfo(string raw, out PostScheduleInfo info)
        {
            info = new PostScheduleInfo();
            if (string.IsNullOrWhiteSpace(raw)) return false;
            try
            {
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var days = System.Text.Json.JsonSerializer.Deserialize<List<DailyTimeSlotsDto>>(raw, opts);

                if (days == null || !days.Any()) return false;

                var valid = new List<DateOnly>();
                foreach (var d in days)
                {
                    if (DateOnly.TryParse(d.PickUpDate, out var date))
                    {
                        valid.Add(date);
                    }
                }

                if (!valid.Any()) return false;
                valid.Sort();

                info.SpecificDates = valid;
                info.MinDate = valid.First();
                info.MaxDate = valid.Last();
                return true;
            }
            catch { return false; }
        }
        private sealed class TimeSlotDetailDto
        {
            public string? StartTime { get; set; }
            public string? EndTime { get; set; }
        }
        private sealed class DailyTimeSlotsDto
        {
            public string? DayName { get; set; }
            public string? PickUpDate { get; set; }
            public TimeSlotDetailDto? Slots { get; set; }
        }
        private class PostScheduleInfo
        {
            public DateOnly MinDate { get; set; }
            public DateOnly MaxDate { get; set; }
            public List<DateOnly> SpecificDates { get; set; } = new();
        }
    }
}
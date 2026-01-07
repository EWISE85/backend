using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System.Text.Json;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductAssignService : IProductAssignService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapboxDistanceCacheService _distanceCache;

        public ProductAssignService(IUnitOfWork unitOfWork, IMapboxDistanceCacheService distanceCache)
        {
            _unitOfWork = unitOfWork;
            _distanceCache = distanceCache;
        }

        public async Task<AssignProductResult> AssignProductsAsync(List<Guid> productIds, DateOnly workDate)
        {
            var result = new AssignProductResult();
            var companies = await _unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints");

            if (!companies.Any())
                throw new Exception("Lỗi cấu hình: Chưa có đơn vị thu gom nào trong hệ thống.");

            var allConfigs = await _unitOfWork.SystemConfig.GetAllAsync();

            var sortedConfig = companies.OrderBy(c => c.CompanyId).ToList();

            double totalPercent = sortedConfig.Sum(c => GetConfigValue(allConfigs, c.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0));

            if (Math.Abs(totalPercent - 100) > 0.1)
                throw new Exception($"Lỗi cấu hình: Tổng tỉ lệ phân bổ hiện tại là {totalPercent}%, yêu cầu bắt buộc là 100%.");

            var rangeConfigs = new List<CompanyRangeConfig>();
            double currentPivot = 0.0;

            foreach (var comp in sortedConfig)
            {
                double assignRatio = GetConfigValue(allConfigs, comp.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0);

                var cfg = new CompanyRangeConfig
                {
                    CompanyEntity = comp,
                    AssignRatio = assignRatio, 
                    MinRange = currentPivot
                };
                currentPivot += (assignRatio / 100.0);
                cfg.MaxRange = currentPivot;
                rangeConfigs.Add(cfg);
            }

            var products = await _unitOfWork.Products.GetAllAsync(filter: p => productIds.Contains(p.ProductId));
            if (!products.Any()) throw new Exception("Không tìm thấy sản phẩm nào hợp lệ.");

            foreach (var product in products)
            {
                try
                {
                    var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == product.ProductId);
                    if (post == null)
                    {
                        result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Không tìm thấy bài đăng (Post)" });
                        continue;
                    }

                    if (string.IsNullOrEmpty(post.Address))
                    {
                        result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Địa chỉ bài đăng bị trống" });
                        continue;
                    }

                    var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == post.SenderId && a.Address == post.Address);

                    if (matchedAddress == null || matchedAddress.Iat == null || matchedAddress.Ing == null)
                    {
                        result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Không lấy được tọa độ (Lat/Lng) từ địa chỉ này" });
                        continue;
                    }

                    // QUÉT VỊ TRÍ TRƯỚC -> CHIA TỶ LỆ SAU

                    var validCandidates = new List<ProductAssignCandidate>();

                    foreach (var company in sortedConfig)
                    {
                        var candidate = await FindBestSmallPointForCompanyAsync(company, matchedAddress, allConfigs);
                        if (candidate != null)
                        {
                            validCandidates.Add(candidate);
                        }
                    }

                    ProductAssignCandidate? chosenCandidate = null;
                    string assignNote = "";

                    if (!validCandidates.Any())
                    {
                        result.TotalUnassigned++;

                        result.Details.Add(new
                        {
                            productId = product.ProductId,
                            status = "failed",
                            reason = "Không có đơn vị thu gom gần đây"
                        });

                        product.Status = "Không tìm thấy điểm thu gom";
                        _unitOfWork.Products.Update(product);

                        continue;
                    }
                    else
                    {
                        double magicNumber = GetStableHashRatio(product.ProductId);

                        var targetConfig = rangeConfigs.FirstOrDefault(t => magicNumber >= t.MinRange && magicNumber < t.MaxRange);
                        if (targetConfig == null) targetConfig = rangeConfigs.Last();

                        var targetCandidate = validCandidates.FirstOrDefault(c => c.CompanyId == targetConfig.CompanyEntity.CompanyId);

                        if (targetCandidate != null)
                        {
                            chosenCandidate = targetCandidate;
                            assignNote = $"Đúng tuyến - Tỉ lệ {targetConfig.AssignRatio}%";
                        }
                        else
                        {
                            chosenCandidate = validCandidates.OrderBy(c => c.RoadKm).First();
                            assignNote = "Trái tuyến - Chọn kho gần nhất";
                        }
                    }

                    if (chosenCandidate != null)
                    {
                        post.CollectionCompanyId = chosenCandidate.CompanyId;
                        post.AssignedSmallPointId = chosenCandidate.SmallPointId;
                        post.DistanceToPointKm = chosenCandidate.RoadKm;
                        _unitOfWork.Posts.Update(post);

                        product.SmallCollectionPointId = chosenCandidate.SmallPointId;
                        product.Status = "Chờ gom nhóm";
                        _unitOfWork.Products.Update(product);

                        var history = new ProductStatusHistory
                        {
                            ProductId = product.ProductId,
                            ChangedAt = DateTime.UtcNow,
                            Status = "Chờ gom nhóm",
                            StatusDescription = $"Đã phân bổ về kho: {chosenCandidate.SmallPointId} - {assignNote}"
                        };
                        await _unitOfWork.ProductStatusHistory.AddAsync(history);

                        result.TotalAssigned++;

                        result.Details.Add(new
                        {
                            productId = product.ProductId,
                            companyId = chosenCandidate.CompanyId,
                            smallPointId = chosenCandidate.SmallPointId,
                            roadKm = $"{Math.Round(chosenCandidate.RoadKm, 2):0.00} km",
                            status = "assigned",
                            note = assignNote
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Details.Add(new { productId = product.ProductId, status = "error", message = $"Lỗi hệ thống: {ex.Message}" });
                }
            }

            await _unitOfWork.SaveAsync();

            return result;
        }

        private async Task<ProductAssignCandidate?> FindBestSmallPointForCompanyAsync(Company company, UserAddress address, IEnumerable<SystemConfig> configs)
        {
            ProductAssignCandidate? best = null;
            double minRoadKm = double.MaxValue;

            if (company.SmallCollectionPoints == null) return null;

            foreach (var sp in company.SmallCollectionPoints)
            {
                double radiusKm = GetConfigValue(configs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 10); 
                double maxRoadKm = GetConfigValue(configs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 15); 

                double hvDistance = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
                if (hvDistance > radiusKm) continue;

                double roadKm = await _distanceCache.GetRoadDistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
                if (roadKm > maxRoadKm) continue;

                if (roadKm < minRoadKm)
                {
                    minRoadKm = roadKm;
                    best = new ProductAssignCandidate
                    {
                        ProductId = Guid.Empty,
                        CompanyId = company.CompanyId,
                        SmallPointId = sp.SmallCollectionPointsId,
                        RoadKm = roadKm,
                        HaversineKm = hvDistance
                    };
                }
            }
            return best;
        }

        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.SmallCollectionPointId == pointId &&
                pointId != null);

            if (config == null && companyId != null)
            {
                config = configs.FirstOrDefault(x =>
                x.Key == key.ToString() &&
                x.CompanyId == companyId &&
                x.SmallCollectionPointId == null);
            }

            if (config == null)
            {
                config = configs.FirstOrDefault(x =>
               x.Key == key.ToString() &&
               x.CompanyId == null &&
               x.SmallCollectionPointId == null);
            }

            if (config != null && double.TryParse(config.Value, out double result))
            {
                return result;
            }
            return defaultValue;
        }

        private double GetStableHashRatio(Guid id)
        {
            int hash = id.GetHashCode();
            if (hash < 0) hash = -hash;
            return (hash % 10000) / 10000.0;
        }

        public async Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate)
        {
            var posts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.Product != null && p.Product.Status == "Chờ phân kho",
                includeProperties: "Product,Sender,Product.Category,Product.Brand"
            );

            var result = new List<ProductByDateModel>();

            foreach (var post in posts)
            {
                if (!TryParseScheduleInfo(post.ScheduleJson!, out var dates))
                    continue;

                if (!dates.Contains(workDate))
                    continue;

                string displayAddr = post.Address ?? "Chưa cập nhật";

                result.Add(new ProductByDateModel
                {
                    ProductId = post.Product!.ProductId,
                    PostId = post.PostId,
                    CategoryName = post.Product.Category?.Name ?? "Không xác định",
                    BrandName = post.Product.Brand?.Name ?? "Không xác định",
                    UserName = post.Sender?.Name ?? "Không xác định",
                    Address = displayAddr
                });
            }

            return result;
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
            catch { return false; }
        }

        private class CompanyRangeConfig
        {
            public Company CompanyEntity { get; set; } = null!;
            public double AssignRatio { get; set; } 
            public double MinRange { get; set; }
            public double MaxRange { get; set; }
        }

        private class ScheduleDayDto
        {
            public string? PickUpDate { get; set; }
            public object? Slots { get; set; }
        }
    }
}
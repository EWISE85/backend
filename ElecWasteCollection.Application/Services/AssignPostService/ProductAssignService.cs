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
            var companies = await _unitOfWork.CollectionCompanies.GetAllAsync(includeProperties: "SmallCollectionPoints");

            if (!companies.Any())
                throw new Exception("Chưa có cấu hình company.");

            var sortedConfig = companies.OrderBy(c => c.CollectionCompanyId).ToList();
            double totalPercent = sortedConfig.Sum(c => c.AssignRatio);

            if (Math.Abs(totalPercent - 100) > 0.1)
                throw new Exception($"Tổng tỉ lệ phần trăm hiện tại là {totalPercent}%, cần phải là 100%");

            var rangeConfigs = new List<CompanyRangeConfig>();
            double currentPivot = 0.0;

            foreach (var comp in sortedConfig)
            {
                var cfg = new CompanyRangeConfig
                {
                    CompanyEntity = comp,
                    MinRange = currentPivot
                };
                currentPivot += (comp.AssignRatio / 100.0);
                cfg.MaxRange = currentPivot;
                rangeConfigs.Add(cfg);
            }
            var products = await _unitOfWork.Products.GetAllAsync(filter: p => productIds.Contains(p.ProductId));
            if (!products.Any()) throw new Exception("Không có product hợp lệ.");

            foreach (var product in products)
            {
                try
                {
                    var post = await _unitOfWork.Posts.GetAsync(p => p.ProductId == product.ProductId);
                    if (post == null) continue;

                    if (string.IsNullOrEmpty(post.Address))
                    {
                        result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Post address is empty" });
                        continue;
                    }
                    var matchedAddress = await _unitOfWork.UserAddresses.GetAsync(a => a.UserId == post.SenderId && a.Address == post.Address);

                    if (matchedAddress == null || matchedAddress.Iat == null || matchedAddress.Ing == null)
                    {
                        result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Coordinates not found for this address" });
                        continue;
                    }
            
                    double magicNumber = GetStableHashRatio(product.ProductId);

                    var targetConfig = rangeConfigs.FirstOrDefault(t => magicNumber >= t.MinRange && magicNumber < t.MaxRange);
                    if (targetConfig == null) targetConfig = rangeConfigs.Last();

                    ProductAssignCandidate? chosenCandidate = null;

                    var targetCandidate = await FindBestSmallPointForCompanyAsync(targetConfig.CompanyEntity, matchedAddress);

                    if (targetCandidate != null)
                    {
                        chosenCandidate = targetCandidate;
                    }
                    else
                    {
                        double bestDistance = double.MaxValue;
                        foreach (var otherConfig in rangeConfigs.Where(c => c.CompanyEntity.CollectionCompanyId != targetConfig.CompanyEntity.CollectionCompanyId))
                        {
                            var candidate = await FindBestSmallPointForCompanyAsync(otherConfig.CompanyEntity, matchedAddress);
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
                        result.Details.Add(new { productId = product.ProductId, status = "no_candidate_available" });
                    }
                    else
                    {
                        post.CollectionCompanyId = chosenCandidate.CompanyId;
                        post.AssignedSmallPointId = chosenCandidate.SmallPointId;
                        post.DistanceToPointKm = chosenCandidate.RoadKm;

                        _unitOfWork.Posts.Update(post);

                        product.SmallCollectionPointId = chosenCandidate.SmallPointId;
                        _unitOfWork.Products.Update(product);

                        result.TotalAssigned++;
                        string note = (chosenCandidate.CompanyId == targetConfig.CompanyEntity.CollectionCompanyId) ? "Target Match" : "Fallback Geo";

                        result.Details.Add(new
                        {
                            productId = product.ProductId,
                            companyId = chosenCandidate.CompanyId,
                            smallPointId = chosenCandidate.SmallPointId,
                            roadKm = $"{Math.Round(chosenCandidate.RoadKm, 2):0.00} km",
                            status = "assigned",
                            note = note
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.Details.Add(new { productId = product.ProductId, status = "error", message = ex.Message });
                }
            }

            await _unitOfWork.SaveAsync();

            return result;
        }

        private async Task<ProductAssignCandidate?> FindBestSmallPointForCompanyAsync(CollectionCompany company, UserAddress address)
        {
            ProductAssignCandidate? best = null;
            double minRoadKm = double.MaxValue;

            if (company.SmallCollectionPoints == null) return null;

            foreach (var sp in company.SmallCollectionPoints)
            {
                // Tinh toán khoảng cách Haversine trước
                double hvDistance = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
                if (hvDistance > sp.RadiusKm) continue;

                // Tính toán khoảng cách đường bộ (road distance) sử dụng Mapbox API với caching
                double roadKm = await _distanceCache.GetRoadDistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
                if (roadKm > sp.MaxRoadDistanceKm) continue;

                if (roadKm < minRoadKm)
                {
                    minRoadKm = roadKm;
                    best = new ProductAssignCandidate
                    {
                        ProductId = Guid.Empty,
                        CompanyId = company.CollectionCompanyId,
                        SmallPointId = sp.SmallCollectionPointsId,
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
            var posts = await _unitOfWork.Posts.GetAllAsync(
                filter: p => p.Product != null && p.Product.Status == "Chờ gom nhóm",
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
                    CategoryName = post.Product.Category?.Name ?? "Unknown Category",
                    BrandName = post.Product.Brand?.Name ?? "Unknown Brand",
                    UserName = post.Sender?.Name ?? "Unknown User",
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
            public CollectionCompany CompanyEntity { get; set; } = null!;
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
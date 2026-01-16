using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent; // Cần thêm cái này cho Thread-Safe
using System.Text.Json;
using ElecWasteCollection.Application.IServices;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductAssignService : IProductAssignService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IUnitOfWork _unitOfWorkForGet;

        public ProductAssignService(
            IServiceScopeFactory scopeFactory,
            IUnitOfWork unitOfWorkForGet)
        {
            _scopeFactory = scopeFactory;
            _unitOfWorkForGet = unitOfWorkForGet;
        }

        public void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId)
        {
            Task.Run(async () =>
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var distanceCache = scope.ServiceProvider.GetRequiredService<IMapboxDistanceCacheService>();
                    var notifService = scope.ServiceProvider.GetRequiredService<IWebNotificationService>();

                    try
                    {
                        var result = await AssignProductsLogicInternal(unitOfWork, distanceCache, productIds, workDate);

                        var summaryData = new
                        {
                            Action = "ASSIGN_COMPLETED",
                            TotalRequested = productIds.Count,
                            Success = result.TotalAssigned,
                            Failed = result.Details.Count(x => (string)x.GetType().GetProperty("status")?.GetValue(x, null)! == "failed"),
                            Unassigned = result.TotalUnassigned
                        };

                        var notification = new Notifications
                        {
                            NotificationId = Guid.NewGuid(),
                            Body = $"Đã xử lý xong {productIds.Count} sản phẩm. Thành công: {result.TotalAssigned}.",
                            Title = "Phân bổ hoàn tất",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false,
                            UserId = Guid.Parse(userId),
                        };
                        await unitOfWork.Notifications.AddAsync(notification);
                        await unitOfWork.SaveAsync();

                        await notifService.SendNotificationAsync(
                            userId: userId,
                            title: "Phân bổ hoàn tất",
                            message: $"Đã xử lý xong {productIds.Count} sản phẩm. Thành công: {result.TotalAssigned}.",
                            type: "success",
                            data: summaryData
                        );
                    }
                    catch (Exception ex)
                    {
                        await notifService.SendNotificationAsync(
                            userId: userId,
                            title: "Phân bổ thất bại",
                            message: "Có lỗi xảy ra trong quá trình xử lý ngầm.",
                            type: "error",
                            data: new { Error = ex.Message }
                        );
                    }
                }
            });
        }

        // =========================================================================
        // PHẦN 2: LOGIC XỬ LÝ CHÍNH (SONG SONG - TỐC ĐỘ CAO)
        // =========================================================================
        private async Task<AssignProductResult> AssignProductsLogicInternal( IUnitOfWork unitOfWork, IMapboxDistanceCacheService distanceCache, List<Guid> productIds, DateOnly workDate)
        {
            var result = new AssignProductResult();

            var companies = await unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints");
            if (!companies.Any()) throw new Exception("Lỗi cấu hình: Chưa có đơn vị thu gom nào.");

            var allConfigs = await unitOfWork.SystemConfig.GetAllAsync();
            var sortedConfig = companies.OrderBy(c => c.CompanyId).ToList();

            var rangeConfigs = new List<CompanyRangeConfig>();
            double currentPivot = 0.0;
            foreach (var comp in sortedConfig)
            {
                double assignRatio = GetConfigValue(allConfigs, comp.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0);
                var cfg = new CompanyRangeConfig { CompanyEntity = comp, AssignRatio = assignRatio, MinRange = currentPivot };
                currentPivot += (assignRatio / 100.0);
                cfg.MaxRange = currentPivot;
                rangeConfigs.Add(cfg);
            }

            var products = await unitOfWork.Products.GetAllAsync(filter: p => productIds.Contains(p.ProductId));
            var postIds = products.Select(p => p.ProductId).ToList();
            var posts = await unitOfWork.Posts.GetAllAsync(p => postIds.Contains(p.ProductId));
            var senderIds = posts.Select(p => p.SenderId).Distinct().ToList();
            var addresses = await unitOfWork.UserAddresses.GetAllAsync(a => senderIds.Contains(a.UserId));

            // CHUẨN BỊ CHO XỬ LÝ SONG SONG (THREAD-SAFE)
            // Sử dụng ConcurrentBag thay vì List thường vì nhiều luồng sẽ ghi vào cùng lúc
            var historyListBag = new ConcurrentBag<ProductStatusHistory>();
            var detailsBag = new ConcurrentBag<object>();

            // Biến đếm Thread-safe
            int totalAssigned = 0;
            int totalUnassigned = 0;

            var allSmallPoints = sortedConfig
                .SelectMany(c => c.SmallCollectionPoints ?? Enumerable.Empty<SmallCollectionPoints>())
                .ToList();

            // CẤU HÌNH SONG SONG
            // Cho phép chạy tối đa 8 luồng cùng lúc (An toàn cho Mapbox Free)
            // Nếu muốn nhanh hơn có thể tăng lên 10-12, nhưng cẩn thận lỗi 429
            var semaphore = new SemaphoreSlim(8);

            var tasks = products.Select(async product =>
            {
                // Chờ đến lượt (nếu đang có 8 luồng chạy thì luồng thứ 9 phải đợi)
                await semaphore.WaitAsync();
                try
                {
                    var post = posts.FirstOrDefault(p => p.ProductId == product.ProductId);
                    if (post == null || string.IsNullOrEmpty(post.Address))
                    {
                        detailsBag.Add(new { productId = product.ProductId, status = "failed", reason = "Info Invalid" });
                        return;
                    }

                    var matchedAddress = addresses.FirstOrDefault(a => a.UserId == post.SenderId && a.Address == post.Address);
                    if (matchedAddress == null || matchedAddress.Iat == null || matchedAddress.Ing == null)
                    {
                        detailsBag.Add(new { productId = product.ProductId, status = "failed", reason = "No Coords" });
                        return;
                    }

                    // --- BƯỚC A: MATRIX API ---
                    Dictionary<string, double> matrixDistances = new Dictionary<string, double>();
                    if (allSmallPoints.Any())
                    {
                        matrixDistances = await distanceCache.GetMatrixDistancesAsync(
                           matchedAddress.Iat.Value,
                           matchedAddress.Ing.Value,
                           allSmallPoints
                       );
                    }

                    var validCandidates = new List<ProductAssignCandidate>();

                    foreach (var company in sortedConfig)
                    {
                        if (company.SmallCollectionPoints == null) continue;
                        ProductAssignCandidate? bestForComp = null;
                        double minRoadKm = double.MaxValue;

                        foreach (var sp in company.SmallCollectionPoints)
                        {
                            double hvDistance = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, matchedAddress.Iat.Value, matchedAddress.Ing.Value);
                            double radiusKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 10);
                            if (hvDistance > radiusKm) continue;

                            double roadKm;
                            if (matrixDistances.TryGetValue(sp.SmallCollectionPointsId, out double kmFromMatrix))
                                roadKm = kmFromMatrix;
                            else
                                roadKm = hvDistance;

                            double maxRoadKm = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 15);
                            if (roadKm > maxRoadKm) continue;

                            if (roadKm < minRoadKm)
                            {
                                minRoadKm = roadKm;
                                bestForComp = new ProductAssignCandidate
                                {
                                    ProductId = product.ProductId,
                                    CompanyId = company.CompanyId,
                                    SmallPointId = sp.SmallCollectionPointsId,
                                    RoadKm = roadKm,
                                    HaversineKm = hvDistance
                                };
                            }
                        }
                        if (bestForComp != null) validCandidates.Add(bestForComp);
                    }

                    ProductAssignCandidate? chosenCandidate = null;
                    string assignNote = "";

                    if (!validCandidates.Any())
                    {
                        Interlocked.Increment(ref totalUnassigned); 
                        detailsBag.Add(new { productId = product.ProductId, status = "failed", reason = "Out of range" });
                        product.Status = ProductStatus.KHONG_TIM_THAY_DIEM_THU_GOM.ToString();
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
                            assignNote = $"Đúng tuyến - {targetConfig.AssignRatio}%";
                        }
                        else
                        {
                            chosenCandidate = validCandidates.OrderBy(c => c.RoadKm).First();
                            assignNote = "Trái tuyến - Gần nhất";
                        }
                    }

                    if (chosenCandidate != null)
                    {
                        post.CollectionCompanyId = chosenCandidate.CompanyId;
                        post.AssignedSmallPointId = chosenCandidate.SmallPointId;
                        post.DistanceToPointKm = chosenCandidate.RoadKm;

                        product.SmallCollectionPointId = chosenCandidate.SmallPointId;
                        product.Status = ProductStatus.CHO_GOM_NHOM.ToString();

                        historyListBag.Add(new ProductStatusHistory
                        {
                            ProductStatusHistoryId = Guid.NewGuid(),
                            ProductId = product.ProductId,
                            ChangedAt = DateTime.UtcNow,
                            Status = ProductStatus.CHO_GOM_NHOM.ToString(),
                            StatusDescription = $"Kho: {chosenCandidate.SmallPointId} - {assignNote}"
                        });

                        Interlocked.Increment(ref totalAssigned);
                        detailsBag.Add(new
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
                    detailsBag.Add(new { productId = product.ProductId, status = "error", message = ex.Message });
                }
                finally
                {
                    semaphore.Release(); 
                }
            });

            await Task.WhenAll(tasks);

            // Gán lại kết quả từ Bag vào Result (vì Bag không có thứ tự, nhưng không quan trọng)
            result.TotalAssigned = totalAssigned;
            result.TotalUnassigned = totalUnassigned;
            result.Details = detailsBag.ToList();

            if (historyListBag.Any())
            {
                // Vì DbContext không Thread-Safe, nên đoạn Save này phải chạy tuần tự ở cuối cùng (rất nhanh)
                foreach (var history in historyListBag)
                {
                    await unitOfWork.ProductStatusHistory.AddAsync(history);
                }
            }
            await unitOfWork.SaveAsync();

            return result;
        }

        public async Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate)
        {
            var posts = await _unitOfWorkForGet.Posts.GetAllAsync(
                filter: p => p.Product != null && p.Product.Status == ProductStatus.CHO_PHAN_KHO.ToString(),
                includeProperties: "Product,Sender,Product.Category,Product.Brand"
            );
            var result = new List<ProductByDateModel>();
            foreach (var post in posts)
            {
                if (!TryParseScheduleInfo(post.ScheduleJson!, out var dates)) continue;
                if (!dates.Contains(workDate)) continue;
                result.Add(new ProductByDateModel
                {
                    ProductId = post.Product!.ProductId,
                    PostId = post.PostId,
                    CategoryName = post.Product.Category?.Name ?? "N/A",
                    BrandName = post.Product.Brand?.Name ?? "N/A",
                    UserName = post.Sender?.Name ?? "N/A",
                    Address = post.Address ?? "N/A"
                });
            }
            return result;
        }

        public async Task<object> GetProductIdsForWorkDateAsync(DateOnly workDate)
        {
            var posts = await _unitOfWorkForGet.Posts.GetAllAsync(
                filter: p => p.Product != null && p.Product.Status == ProductStatus.CHO_PHAN_KHO.ToString(),
                includeProperties: "Product"
            );
            var listIds = new List<string>();
            foreach (var post in posts)
            {
                if (!TryParseScheduleInfo(post.ScheduleJson!, out var dates)) continue;
                if (dates.Contains(workDate)) listIds.Add(post.Product!.ProductId.ToString());
            }
            return new { Total = listIds.Count, List = listIds };
        }


        private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
        {
            var config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.SmallCollectionPointId == pointId && pointId != null);
            if (config == null && companyId != null)
                config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.CompanyId == companyId && x.SmallCollectionPointId == null);
            if (config == null)
                config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.CompanyId == null && x.SmallCollectionPointId == null);
            if (config != null && double.TryParse(config.Value, out double result)) return result;
            return defaultValue;
        }

        private double GetStableHashRatio(Guid id)
        {
            int hash = id.GetHashCode();
            if (hash < 0) hash = -hash;
            return (hash % 10000) / 10000.0;
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
                    if (DateOnly.TryParse(d.PickUpDate, out var date)) dates.Add(date);
                return dates.Any();
            }
            catch { return false; }
        }

        private class CompanyRangeConfig { public Company CompanyEntity { get; set; } = null!; public double AssignRatio { get; set; } public double MinRange { get; set; } public double MaxRange { get; set; } }
        private class ScheduleDayDto { public string? PickUpDate { get; set; } public object? Slots { get; set; } }
    }
}
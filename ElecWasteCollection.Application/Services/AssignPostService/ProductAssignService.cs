using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Text.Json;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;

namespace ElecWasteCollection.Application.Services.AssignPostService
{
    public class ProductAssignService : IProductAssignService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IUnitOfWork _unitOfWork;

        public ProductAssignService(
            IServiceScopeFactory scopeFactory,
            IUnitOfWork unitOfWork)
        {
            _scopeFactory = scopeFactory;
            _unitOfWork = unitOfWork;
        }

        public void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId, List<string>? targetCompanyIds = null)
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
                        // Truyền thêm targetCompanyIds vào logic xử lý nội bộ
                        var result = await AssignProductsLogicInternal(unitOfWork, distanceCache, productIds, workDate, targetCompanyIds);

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
                            Type = NotificationType.System.ToString(),
                            EventId = Guid.Empty
                        };
                        await unitOfWork.Notifications.AddAsync(notification);

                        await notifService.SendNotificationAsync(
                            userId: userId,
                            title: "Phân bổ hoàn tất",
                            message: $"Đã xử lý xong {productIds.Count} sản phẩm. Thành công: {result.TotalAssigned}.",
                            type: "success",
                            data: summaryData
                        );
						if (result.WarehouseAllocations != null && result.WarehouseAllocations.Any())
						{
							foreach (var stat in result.WarehouseAllocations)
							{
								// Tạo thông báo
								string msg = $"Kho {stat.WarehouseName} vừa nhận được {stat.AssignedCount} sản phẩm.";

								// Gửi SignalR (await trực tiếp -> tuần tự)
								await notifService.SendNotificationAsync(
									userId: stat.AdminWarehouseId,
									title: "Hàng về kho",
									message: msg,
									type: "info",
									data: new
									{
										Action = "WAREHOUSE_RECEIVED",
										WarehouseId = stat.WarehouseId,
										Count = stat.AssignedCount
									}
								);
								var adminWarehouseNotification = new Notifications
								{
									NotificationId = Guid.NewGuid(),
									Body = $"Kho {stat.WarehouseName} vừa nhận được {stat.AssignedCount} sản phẩm.",
									Title = "Hàng về kho",
									CreatedAt = DateTime.UtcNow,
									IsRead = false,
									UserId = Guid.Parse(stat.AdminWarehouseId),
									Type = NotificationType.System.ToString(),
									EventId = Guid.Empty
								};
								await unitOfWork.Notifications.AddAsync(adminWarehouseNotification);

								
							}
						}
						await unitOfWork.SaveAsync();

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

        private async Task<AssignProductResult> AssignProductsLogicInternal(IUnitOfWork unitOfWork, IMapboxDistanceCacheService distanceCache, List<Guid> productIds, DateOnly workDate, List<string>? targetCompanyIds)
        {
            var result = new AssignProductResult();

            var allCategories = await unitOfWork.Categories.GetAllAsync();
            var categoryMap = allCategories.ToDictionary(c => c.CategoryId, c => c.ParentCategoryId ?? c.CategoryId);
            var allCompanies = await unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints,CompanyRecyclingCategories");
            var allConfigs = await unitOfWork.SystemConfig.GetAllAsync();
            var recyclingCompanies = allCompanies.Where(c => c.CompanyType == CompanyType.CTY_TAI_CHE.ToString()).ToList();

            var collectionCompanies = allCompanies.Where(c => c.CompanyType == CompanyType.CTY_THU_GOM.ToString()).OrderBy(c => c.CompanyId).ToList();
            var activeCompanies = targetCompanyIds?.Any() == true
                ? collectionCompanies.Where(c => targetCompanyIds.Contains(c.CompanyId)).ToList()
                : collectionCompanies;

            double sumOfRatios = activeCompanies.Sum(comp => GetConfigValue(allConfigs, comp.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0));
            if (sumOfRatios <= 0) throw new Exception("Không có đơn vị thu gom khả dụng hoặc tổng tỉ lệ bằng 0.");

            var rangeConfigs = new List<CompanyRangeConfig>();
            double currentPivot = 0.0;
            foreach (var comp in activeCompanies)
            {
                double originalRatio = GetConfigValue(allConfigs, comp.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0);
                if (originalRatio > 0)
                {
                    double normalizedRatio = originalRatio / sumOfRatios;
                    var cfg = new CompanyRangeConfig { CompanyEntity = comp, AssignRatio = originalRatio, MinRange = currentPivot };
                    currentPivot += normalizedRatio;
                    cfg.MaxRange = (comp == activeCompanies.Last()) ? 1.0 : currentPivot;
                    rangeConfigs.Add(cfg);
                }
            }

            var products = await unitOfWork.Products.GetAllAsync(p => productIds.Contains(p.ProductId));
            var posts = await unitOfWork.Posts.GetAllAsync(p => productIds.Contains(p.ProductId));
            var addresses = await unitOfWork.UserAddresses.GetAllAsync(a => posts.Select(x => x.SenderId).Contains(a.UserId));

            var historyBag = new ConcurrentBag<ProductStatusHistory>();
            var detailsBag = new ConcurrentBag<object>();
            var trackingBag = new ConcurrentBag<AssignmentTracker>();
            int totalAssigned = 0; int totalUnassigned = 0;

            foreach (var product in products)
            {
                var post = posts.FirstOrDefault(p => p.ProductId == product.ProductId);
                if (post == null || string.IsNullOrEmpty(post.Address)) continue;
                if (!categoryMap.TryGetValue(product.CategoryId, out Guid rootCateId)) continue;

                var matchedAddr = addresses.FirstOrDefault(a => a.UserId == post.SenderId && a.Address == post.Address);
                if (matchedAddr?.Iat == null || matchedAddr?.Ing == null) continue;

                var candidates = new List<ProductAssignCandidate>();
                foreach (var rc in rangeConfigs)
                {
                    foreach (var sp in rc.CompanyEntity.SmallCollectionPoints.Where(s => s.Status == CompanyStatus.DANG_HOAT_DONG.ToString()))
                    {
                        var rComp = recyclingCompanies.FirstOrDefault(c => c.CompanyId == sp.RecyclingCompanyId);
                        if (rComp?.CompanyRecyclingCategories.Any(crc => crc.CategoryId == rootCateId) != true) continue;

                        double hvDist = Math.Round(GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, matchedAddr.Iat.Value, matchedAddr.Ing.Value), 3);
                        double radius = GetConfigValue(allConfigs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 10);

                        if (hvDist <= radius)
                        {
                            candidates.Add(new ProductAssignCandidate
                            {
                                ProductId = product.ProductId,
                                SmallPointId = sp.SmallCollectionPointsId,
                                CompanyId = rc.CompanyEntity.CompanyId,
                                HaversineKm = hvDist
                            });
                        }
                    }
                }

                if (!candidates.Any())
                {
                    MarkAsUnassigned(product, detailsBag, ref totalUnassigned, "Không tìm thấy kho trong bán kính chim bay");
                    continue;
                }

                double magic = GetStableHashRatio(product.ProductId);
                var targetRC = rangeConfigs.FirstOrDefault(r => magic >= r.MinRange && magic < r.MaxRange) ?? rangeConfigs.Last();

                var chosen = candidates.Where(c => c.CompanyId == targetRC.CompanyEntity.CompanyId).OrderBy(c => c.HaversineKm).FirstOrDefault()
                              ?? candidates.OrderBy(c => c.HaversineKm).First();

                chosen.RoadKm = chosen.HaversineKm;
                AssignSuccess(product, post, chosen, "Tự động phân bổ (Chờ cập nhật RoadKm thực tế)", historyBag, detailsBag, ref totalAssigned, workDate);

                trackingBag.Add(new AssignmentTracker { ProductId = product.ProductId, SmallCollectionPointId = chosen.SmallPointId });
            }

            foreach (var history in historyBag) await unitOfWork.ProductStatusHistory.AddAsync(history);

            result.TotalAssigned = totalAssigned;
            result.TotalUnassigned = totalUnassigned;
            result.Details = detailsBag.ToList();

            if (!trackingBag.IsEmpty)
            {
                var assignedIds = trackingBag.Select(t => t.SmallCollectionPointId).Distinct().ToList();
                var warehouses = await unitOfWork.SmallCollectionPoints.GetAllAsync(p => assignedIds.Contains(p.SmallCollectionPointsId));
                var warehouseMap = warehouses.ToDictionary(w => w.SmallCollectionPointsId, w => w.Name);
                var adminUsers = await unitOfWork.Users.GetAllAsync(u => u.Role == UserRole.AdminWarehouse.ToString() && assignedIds.Contains(u.SmallCollectionPointId) && u.Status == UserStatus.DANG_HOAT_DONG.ToString());
                var adminDict = adminUsers.GroupBy(u => u.SmallCollectionPointId).ToDictionary(g => g.Key, g => g.First().UserId.ToString());

                result.WarehouseAllocations = trackingBag.GroupBy(t => t.SmallCollectionPointId).Select(g => new WarehouseAllocationStats
                {
                    WarehouseId = g.Key,
                    WarehouseName = warehouseMap.GetValueOrDefault(g.Key, "N/A"),
                    AssignedCount = g.Count(),
                    AdminWarehouseId = adminDict.GetValueOrDefault(g.Key)
                }).Where(x => !string.IsNullOrEmpty(x.AdminWarehouseId)).ToList();
            }

            await unitOfWork.SaveAsync();

            _ = Task.Run(() => UpdateRealDistanceAsync(productIds));

            return result;
        }
        private async Task UpdateRealDistanceAsync(List<Guid> productIds)
        {
            await Task.Delay(3000); 
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var mapboxClient = scope.ServiceProvider.GetRequiredService<MapboxDirectionsClient>();

            var posts = await unitOfWork.Posts.GetAllAsync(
                p => productIds.Contains(p.ProductId) && !string.IsNullOrEmpty(p.AssignedSmallPointId),
                includeProperties: "AssignedSmallPoint"
            );

            var senderIds = posts.Select(p => p.SenderId).Distinct().ToList();
            var addresses = await unitOfWork.UserAddresses.GetAllAsync(a => senderIds.Contains(a.UserId));

            int updatedCount = 0;
            int keptOldCount = 0;

            var semaphore = new SemaphoreSlim(3);

            var tasks = posts.Select(async post =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var addr = addresses.FirstOrDefault(a => a.UserId == post.SenderId && a.Address == post.Address);
                    if (addr?.Iat == null || addr?.Ing == null || post.AssignedSmallPoint == null)
                    {
                        Interlocked.Increment(ref keptOldCount);
                        return;
                    }

                    var route = await mapboxClient.GetRouteAsync(addr.Iat.Value, addr.Ing.Value, post.AssignedSmallPoint.Latitude, post.AssignedSmallPoint.Longitude);

                    if (route != null && route.Distance > 0)
                    {
                        // Làm tròn 2 số lẻ cho thực tế
                        post.DistanceToPointKm = Math.Round(route.Distance / 1000.0, 2);

                        unitOfWork.Posts.Update(post);
                        Interlocked.Increment(ref updatedCount);
                    }
                    else
                    {
                        Interlocked.Increment(ref keptOldCount);
                    }
                }
                finally
                {
                    semaphore.Release();
                    await Task.Delay(200); 
                }
            });

            await Task.WhenAll(tasks);
            if (updatedCount > 0) await unitOfWork.SaveAsync();

            Console.WriteLine($"[FINISH] Cap nhat: {updatedCount} don | Giu nguyen: {keptOldCount} don.");
        }

        public async Task<RejectProductResponse> RejectProductsAsync(RejectProductRequest request)
        {
            var response = new RejectProductResponse
            {
                Data = new RejectProductData { TotalProcessed = request.ProductIds.Count }
            };

            try
            {
                var products = await _unitOfWork.Products.GetAllAsync(
                    p => request.ProductIds.Contains(p.ProductId)
                );

                foreach (var productId in request.ProductIds)
                {
                    var product = products.FirstOrDefault(p => p.ProductId == productId);
                    var detail = new RejectDetail { ProductId = productId };

                    if (product == null)
                    {
                        detail.Status = "failed";
                        detail.Message = "Sản phẩm không tồn tại trong hệ thống.";
                        response.Data.Details.Add(detail);
                        continue;
                    }

                    if (product.SmallCollectionPointId != request.SmallCollectionPointId)
                    {
                        detail.Status = "failed";
                        detail.Message = "Sản phẩm này không thuộc quyền quản lý của kho bạn.";
                        response.Data.Details.Add(detail);
                        continue;
                    }

                    product.Status = ProductStatus.CHO_PHAN_KHO.ToString();
                    product.SmallCollectionPointId = null; 

                    var history = new ProductStatusHistory
                    {
                        ProductStatusHistoryId = Guid.NewGuid(),
                        ProductId = product.ProductId,
                        Status = ProductStatus.CHO_PHAN_KHO.ToString(),
                        StatusDescription = $"Kho trả hàng. Lý do: {request.Reason}",
                        ChangedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.ProductStatusHistory.AddAsync(history);

                    detail.Status = "success";
                    detail.NewProductStatus = "Chờ phân kho";
                    detail.Message = "Sản phẩm đã được đưa lại vào hàng đợi phân bổ.";

                    response.Data.TotalSuccess++;
                    response.Data.Details.Add(detail);
                }

                if (response.Data.TotalSuccess > 0)
                {
                    await _unitOfWork.SaveAsync();
                }

                response.Success = true;
                response.Message = $"Đã hoàn trả {response.Data.TotalSuccess} sản phẩm về trạng thái chờ phân bổ.";
                response.Data.TotalFailed = response.Data.TotalProcessed - response.Data.TotalSuccess;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Đã xảy ra lỗi trong quá trình xử lý: " + ex.Message;
            }

            return response;
        }

        public async Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate)
        {
            var posts = await _unitOfWork.Posts.GetAllAsync(
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
            var posts = await _unitOfWork.Posts.GetAllAsync(
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

        private void MarkAsUnassigned(Products product, ConcurrentBag<object> details, ref int unassignedCount, string reason)
        {
            Interlocked.Increment(ref unassignedCount);
            product.Status = ProductStatus.KHONG_TIM_THAY_DIEM_THU_GOM.ToString();
            details.Add(new { productId = product.ProductId, status = "failed", reason = reason });
        }

        private void AssignSuccess(Products product, Post post, ProductAssignCandidate chosen, string note, ConcurrentBag<ProductStatusHistory> historyBag, ConcurrentBag<object> details, ref int assignedCount, DateOnly workDate)
        {
            product.SmallCollectionPointId = chosen.SmallPointId;
            product.Status = ProductStatus.CHO_GOM_NHOM.ToString();
            product.AssignedAt = workDate;
            post.AssignedSmallPointId = chosen.SmallPointId;
            post.CollectionCompanyId = chosen.CompanyId;
            post.DistanceToPointKm = chosen.RoadKm;

            historyBag.Add(new ProductStatusHistory
            {
                ProductStatusHistoryId = Guid.NewGuid(),
                ProductId = product.ProductId,
                ChangedAt = DateTime.UtcNow,
                Status = ProductStatus.CHO_GOM_NHOM.ToString(),
                StatusDescription = $"Kho: {chosen.SmallPointId} - {note}"
            });

            Interlocked.Increment(ref assignedCount);
            details.Add(new
            {
                productId = product.ProductId,
                status = "assigned",
                point = chosen.SmallPointId,
                distance = $"{Math.Round(chosen.RoadKm, 2)}km",
                note = note
            });
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
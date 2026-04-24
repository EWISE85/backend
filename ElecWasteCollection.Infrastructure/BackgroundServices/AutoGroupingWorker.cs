using ElecWasteCollection.Application.Interfaces;
using ElecWasteCollection.Application.Model.GroupModel;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
    public class AutoGroupingWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoGroupingWorker> _logger;

        public AutoGroupingWorker(IServiceProvider serviceProvider, ILogger<AutoGroupingWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[SYSTEM] AutoGroupingWorker is started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessAutoGroupingCycleAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ERROR] Lỗi trong chu kỳ quét AutoGrouping.");
                }

                // Kiểm tra mỗi phút một lần
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ProcessAutoGroupingCycleAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var groupingService = scope.ServiceProvider.GetRequiredService<IGroupingService>();

            var now = DateTime.Now;
            string currentTimeStr = now.ToString("HH:mm");
            var today = DateOnly.FromDateTime(now);

            // 1. Lấy tất cả cấu hình AutoGrouping đang hoạt động
            var configs = await unitOfWork.SystemConfig.GetAllAsync(c =>
                c.GroupName == "AutoGrouping" && c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString());

            // 2. Tìm danh sách các trạm đã bật tính năng
            var enabledPointIds = configs
                .Where(c => c.Key == SystemConfigKey.AUTO_GROUP_ENABLED.ToString() && c.Value.ToLower() == "true")
                .Select(c => c.CollectionUnitId)
                .Distinct()
                .ToList();

            foreach (var pointId in enabledPointIds)
            {
                // Lấy giờ chạy và ngưỡng tải trọng của trạm này
                var runTime = configs.FirstOrDefault(c => c.CollectionUnitId == pointId && c.Key == SystemConfigKey.AUTO_GROUP_TIME.ToString())?.Value ?? "07:00";
                var thresholdStr = configs.FirstOrDefault(c => c.CollectionUnitId == pointId && c.Key == SystemConfigKey.AUTO_GROUP_LOAD_THRESHOLD.ToString())?.Value ?? "80";

                double.TryParse(thresholdStr, out double threshold);

                if (runTime == currentTimeStr)
                {
                    _logger.LogInformation("[AUTO] Khởi động gom nhóm tự động cho trạm {id} | Giờ: {time} | Ngưỡng tải: {th}%", pointId, runTime, threshold);
                    await ExecuteGroupingForPointAsync(groupingService, unitOfWork, pointId, today, threshold);
                }
            }
        }

        private async Task ExecuteGroupingForPointAsync(IGroupingService groupingService, IUnitOfWork unitOfWork, string pointId, DateOnly today, double threshold)
        {
            try
            {
                // Bước 1: Lấy sản phẩm đang đợi CHO_GOM_NHOM
                var pendingProducts = await unitOfWork.Products.GetAllAsync(p =>
                    p.CollectionUnitId == pointId && p.Status == ProductStatus.CHO_GOM_NHOM.ToString());

                if (!pendingProducts.Any())
                {
                    _logger.LogInformation("[INFO] Trạm {id} không có sản phẩm nào cần xử lý.", pointId);
                    return;
                }

                // Bước 2: Lấy danh sách xe khả dụng tại trạm
                var vehicles = await groupingService.GetVehiclesBySmallPointAsync(pointId, today);
                if (!vehicles.Any())
                {
                    _logger.LogWarning("[WARN] Trạm {id} có {count} sản phẩm nhưng không có xe rảnh.", pointId, pendingProducts.Count());
                    return;
                }

                // Bước 3: Chạy Simulation (Pre-Assign) với LoadThreshold cấu hình
                var simulation = await groupingService.PreAssignAsync(new PreAssignRequest
                {
                    CollectionPointId = pointId,
                    WorkDate = today,
                    LoadThresholdPercent = threshold,
                    VehicleIds = vehicles.Select(v => v.VehicleId).ToList(),
                    ProductIds = pendingProducts.Select(p => p.ProductId).ToList()
                });

                if (simulation.Days == null || !simulation.Days.Any())
                {
                    _logger.LogInformation("[INFO] Thuật toán không tìm được phương án chia xe tối ưu cho trạm {id}.", pointId);
                    return;
                }

                // Bước 4: Chuyển dữ liệu vào bộ nhớ Staging (InMemoryStaging)
                await groupingService.AssignDayAsync(new AssignDayRequest
                {
                    CollectionPointId = pointId,
                    WorkDate = today,
                    Assignments = simulation.Days.Select(d => new VehicleAssignmentDetail
                    {
                        VehicleId = d.SuggestedVehicle.Id,
                        ProductIds = d.Products.Select(p => p.ProductId).ToList()
                    }).ToList()
                });

                // Bước 5: Chốt kết quả vào DB và tạo tuyến chính thức
                await groupingService.GroupByCollectionPointAsync(new GroupingByPointRequest
                {
                    CollectionPointId = pointId,
                    SaveResult = true
                });

                _logger.LogInformation("[SUCCESS] Trạm {id} đã hoàn tất gom nhóm và tạo tuyến tự động cho {v} xe.", pointId, simulation.Days.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[ERROR] Lỗi xử lý trạm {id} trong quá trình thực thi AutoGrouping.", pointId);
            }
        }
    }
}
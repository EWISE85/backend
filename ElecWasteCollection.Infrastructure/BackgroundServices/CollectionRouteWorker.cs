using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
	public class CollectionRouteWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<CollectionRouteWorker> _logger;

		public CollectionRouteWorker(IServiceProvider serviceProvider, ILogger<CollectionRouteWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Route Status Worker đang khởi động...");

			while (!stoppingToken.IsCancellationRequested)
			{
				int targetHour = 6;
				int targetMinute = 0;

				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						var systemConfigService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();

						// 1. Lấy cấu hình giờ chạy từ Database
						var systemConfig = await systemConfigService.GetSystemConfigByKey(SystemConfigKey.TIME_TO_CHANGE_STATUS_ROUTE.ToString());

						if (systemConfig != null)
						{
							if (TimeSpan.TryParse(systemConfig.Value, out TimeSpan timeConfig))
							{
								targetHour = timeConfig.Hours;
								targetMinute = timeConfig.Minutes;
							}
						}
					}

					// 2. Tính toán thời gian Delay cho đến lần chạy tiếp theo
					var now = DateTime.Now;
					var nextRunTime = new DateTime(now.Year, now.Month, now.Day, targetHour, targetMinute, 0);

					if (now > nextRunTime)
					{
						nextRunTime = nextRunTime.AddDays(1);
					}

					var delay = nextRunTime - now;
					_logger.LogInformation("Worker sẽ tạm dừng trong {Delay} để chờ đến lần chạy tiếp theo lúc {NextRunTime}", delay, nextRunTime);

					// 3. Chờ cho đến giờ hẹn
					await Task.Delay(delay, stoppingToken);

					// 4. Thực hiện logic nghiệp vụ
					using (var scope = _serviceProvider.CreateScope())
					{
						_logger.LogInformation("Bắt đầu thực hiện tự động cập nhật trạng thái Route vào lúc: {Time}", DateTime.Now);

						var routeService = scope.ServiceProvider.GetRequiredService<ICollectionRouteService>();
						await routeService.AutoStartCollectionRoutesAsync();

						_logger.LogInformation("Hoàn thành cập nhật trạng thái Route.");
					}
				}
				catch (OperationCanceledException)
				{
					_logger.LogWarning("Worker đã bị dừng.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xảy ra trong CollectionRouteWorker.");

					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
		}
	}
}
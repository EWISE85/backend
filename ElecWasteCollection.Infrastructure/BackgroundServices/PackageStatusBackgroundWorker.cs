using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
	public class PackageStatusBackgroundWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<PackageStatusBackgroundWorker> _logger;
		private readonly TimeSpan _scheduledRunTime = new TimeSpan(0, 0, 0);
		public PackageStatusBackgroundWorker(IServiceProvider serviceProvider, ILogger<PackageStatusBackgroundWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Background Service khởi động: Chạy ngay lập tức và lặp lại sau mỗi 24 giờ.");

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await UpdatePackageStatusesAsync();

					_logger.LogInformation("Hoàn thành chu kỳ quét. Sẽ đợi 24 giờ tiếp theo...");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi khi cập nhật trạng thái gói hàng.");
				}

				await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
			}
		}

		private async Task UpdatePackageStatusesAsync()
		{
			using (var scope = _serviceProvider.CreateScope())
			{
				var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

				var config = await unitOfWork.SystemConfig.GetAsync(c => c.Key == SystemConfigKey.TIME_TO_CHANGE_PACKAGE_STATUS.ToString());
				if (config == null || !double.TryParse(config.Value, out double daysLimit))
				{
					_logger.LogWarning("Không tìm thấy cấu hình TIME_TO_CHANGE_PACKAGE_STATUS. Bỏ qua lần này.");
					return;
				}

				var nowUtc = DateTime.UtcNow;

				var packagesToUpdate = await unitOfWork.Packages.GetAllAsync(p =>
					p.Status != PackageStatus.TAI_CHE.ToString() &&
					p.DeliveryHandoverAt.HasValue &&
					p.DeliveryHandoverAt.Value.AddDays(daysLimit) <= nowUtc
				);

				if (packagesToUpdate.Any())
				{
					foreach (var package in packagesToUpdate)
					{
						package.Status = PackageStatus.TAI_CHE.ToString();

						await unitOfWork.PackageStatusHistory.AddAsync(new PackageStatusHistory
						{
							PackageId = package.PackageId,
							Status = PackageStatus.TAI_CHE.ToString(),
							ChangedAt = nowUtc,
							StatusDescription = $"Hệ thống tự động cập nhật sau {daysLimit} ngày kể từ khi bàn giao."
						});

						unitOfWork.Packages.Update(package);
					}

					await unitOfWork.SaveAsync();
					_logger.LogInformation($"Đã tự động chuyển {packagesToUpdate.Count()} gói hàng sang TÁI CHẾ.");
				}
			}
		}
	}
}

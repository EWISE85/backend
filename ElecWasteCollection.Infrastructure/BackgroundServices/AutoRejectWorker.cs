using ElecWasteCollection.Application.IServices;
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
	public class AutoRejectWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<AutoRejectWorker> _logger;

		public AutoRejectWorker(IServiceProvider serviceProvider, ILogger<AutoRejectWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						var postService = scope.ServiceProvider.GetRequiredService<IPostService>();
						await postService.AutoRejectExpiredPostsAsync();
					}

					_logger.LogInformation("Đã quét và cập nhật các bài post hết hạn vào lúc: {time}", DateTimeOffset.Now);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xảy ra khi tự động reject bài post.");
				}

				await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
			}
		}
	}
}

using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
	public class AutoSendNotificationReminderWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<AutoSendNotificationReminderWorker> _logger;

		public AutoSendNotificationReminderWorker(IServiceProvider serviceProvider, ILogger<AutoSendNotificationReminderWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("AutoSendNotificationReminderWorker đang khởi động...");

			var timeZoneId = "SE Asia Standard Time";
			TimeZoneInfo vnTimeZone;
			try { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
			catch { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }

			while (!stoppingToken.IsCancellationRequested)
			{
				int targetHour = 18;
				int targetMinute = 0;

				try
				{
					using (var scope = _serviceProvider.CreateScope())
					{
						var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

						var timeConfig = await unitOfWork.SystemConfig.GetAsync(
							c => c.Key == SystemConfigKey.TIME_TO_SEND_NOTIFICATION_REMINDER.ToString() &&
								 c.Status == SystemConfigStatus.DANG_HOAT_DONG.ToString()
						);

						if (timeConfig != null && TimeSpan.TryParse(timeConfig.Value, out TimeSpan timeSpan))
						{
							targetHour = timeSpan.Hours;
							targetMinute = timeSpan.Minutes;
						}
					}

					var nowVn = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
					var nextRunTimeVn = new DateTime(nowVn.Year, nowVn.Month, nowVn.Day, targetHour, targetMinute, 0);

					if (nowVn > nextRunTimeVn)
					{
						nextRunTimeVn = nextRunTimeVn.AddDays(1);
					}

					var delay = nextRunTimeVn - nowVn;
					_logger.LogInformation("Worker gửi thông báo sẽ tạm dừng trong {Delay} để chờ đến lần chạy tiếp theo lúc {NextRunTimeVn} (Giờ VN)", delay, nextRunTimeVn);

					await Task.Delay(delay, stoppingToken);

					using (var scope = _serviceProvider.CreateScope())
					{
						var currentVnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
						_logger.LogInformation("Bắt đầu thực hiện kiểm tra sản phẩm tồn đọng vào lúc: {Time}", currentVnTime);

						var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
						var productService = scope.ServiceProvider.GetRequiredService<IProductAssignService>();
						var notificationService = scope.ServiceProvider.GetRequiredService<IWebNotificationService>();

						var targetDate = DateOnly.FromDateTime(currentVnTime);

						var unassignedProducts = await productService.GetProductsByWorkDateAsync(targetDate);

						if (unassignedProducts.Count > 0)
						{
							var admins = await unitOfWork.Users.GetAllAsync(
								u => u.Role.Name == UserRole.Admin.ToString(),
								includeProperties: "Role"
							);

							if (admins != null && admins.Any())
							{
								var title = "Nhắc nhở gấp: Sản phẩm chưa được phân chia";
								var message = $"Hệ thống hiện có {unassignedProducts.Count} sản phẩm cần được chia trong HÔM NAY ({targetDate:dd/MM/yyyy}) nhưng chưa được chia về đơn vị thu gom.";

								foreach (var admin in admins)
								{
									await notificationService.SendNotificationAsync(
										userId: admin.UserId.ToString(),
										title: title,
										message: message,
										type: "UNASSIGNED_PRODUCTS",
										data: new
										{
											TargetDate = targetDate.ToString("yyyy-MM-dd"),
											UnassignedCount = unassignedProducts.Count,
											Action = "UNASSIGNED_PRODUCTS_REMINDER"
										}
									);
									var notificationLog = new Notifications
									{
										NotificationId = Guid.NewGuid(),
										UserId = admin.UserId,
										Title = title,
										Body = message,
										Type = NotificationType.System.ToString(),
										CreatedAt = DateTime.UtcNow,
										IsRead = false,
									};
									await unitOfWork.Notifications.AddAsync(notificationLog);
								}
								await unitOfWork.SaveAsync();

							}
						}

						_logger.LogInformation("Hoàn thành gửi thông báo nhắc nhở.");
					}
				}
				catch (OperationCanceledException)
				{
					_logger.LogWarning("Worker gửi thông báo đã bị dừng.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xảy ra trong AutoSendNotificationReminderWorker.");
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
		}
	}
}
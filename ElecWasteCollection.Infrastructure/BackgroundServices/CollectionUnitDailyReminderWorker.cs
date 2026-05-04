using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Infrastructure.BackgroundServices
{
	public class CollectionUnitDailyReminderWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<CollectionUnitDailyReminderWorker> _logger;
		private readonly JsonSerializerOptions _jsonOptions;

		public CollectionUnitDailyReminderWorker(IServiceProvider serviceProvider, ILogger<CollectionUnitDailyReminderWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
			_jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("SmallPointTodayReminderWorker đang khởi động...");

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
							c => c.Key == SystemConfigKey.TIME_TO_SEND_COLLECTION_UNIT_REMINDER.ToString() && 
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
						nextRunTimeVn = nextRunTimeVn.AddDays(1);

					var delay = nextRunTimeVn - nowVn;
					_logger.LogInformation("Worker nhắc việc trạm sẽ ngủ trong {Delay} đến {NextRunTimeVn}", delay, nextRunTimeVn);

					await Task.Delay(delay, stoppingToken);

					using (var scope = _serviceProvider.CreateScope())
					{
						var currentVnTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, vnTimeZone);
						var targetDate = DateOnly.FromDateTime(currentVnTime);

						var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
						var notificationService = scope.ServiceProvider.GetRequiredService<IWebNotificationService>();

						var allStations = await unitOfWork.CollectionUnits.GetAllAsync(s => s.Status == CollectionUnitStatus.DANG_HOAT_DONG.ToString());

						foreach (var station in allStations)
						{
							var posts = await unitOfWork.Posts.GetAllAsync(
								filter: p => p.Product != null
										  && p.Product.CollectionUnitId == station.CollectionUnitId
										  && p.Product.Status == ProductStatus.CHO_GOM_NHOM.ToString()
							);

							int count = 0;
							foreach (var post in posts)
							{
								if (!string.IsNullOrEmpty(post.ScheduleJson))
								{
									try
									{
										var schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, _jsonOptions);
										if (schedule != null && schedule.Any(s => s.PickUpDate == targetDate))
										{
											count++;
										}
									}
									catch { }
								}
							}

							if (count > 0)
							{
								var stationAdmin = await unitOfWork.Users.GetAsync(
									u => u.CollectionUnitId == station.CollectionUnitId
									  && u.Role.Name == UserRole.AdminWarehouse.ToString(), // Sửa lại đúng Role Manager của bạn
									includeProperties: "Role"
								);

								if (stationAdmin != null)
								{
									var title = "Nhắc nhở việc phân xe";
									var message = $"Đơn vị thu gom {station.Name} có {count} sản phẩm chưa được phân xe trong hôm nay ({targetDate:dd/MM/yyyy}).";

									await notificationService.SendNotificationAsync(
										userId: stationAdmin.UserId.ToString(),
										title: title,
										message: message,
										type: "MAKE_SCHEDULE",
										data: new { Date = targetDate, Count = count }
									);

									await unitOfWork.Notifications.AddAsync(new Notifications
									{
										NotificationId = Guid.NewGuid(),
										UserId = stationAdmin.UserId,
										Title = title,
										Body = message,
										Type = NotificationType.System.ToString(),
										CreatedAt = DateTime.UtcNow,
										IsRead = false
									});
								}
							}
						}
						await unitOfWork.SaveAsync();
						_logger.LogInformation("Hoàn thành gửi thông báo nhắc việc cho các trạm.");
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Lỗi xảy ra trong SmallPointTodayReminderWorker.");
					await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
				}
			}
		}
	}
}

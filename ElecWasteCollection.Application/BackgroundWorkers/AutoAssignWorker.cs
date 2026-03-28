using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ElecWasteCollection.Application.BackgroundWorkers
{
    public class AutoAssignWorker : BackgroundService
    {
        private readonly IServiceProvider _services;

        public AutoAssignWorker(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($"[SYSTEM] {nameof(AutoAssignWorker)} is started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var configService = scope.ServiceProvider.GetRequiredService<ISystemConfigService>();
                    var assignService = scope.ServiceProvider.GetRequiredService<IProductAssignService>();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                    try
                    {
                        var settings = await configService.GetAutoAssignSettingsAsync();

                        if (settings.IsEnabled)
                        {
                            var pendingProducts = await unitOfWork.Products.GetAllAsync(p =>
                                p.Status == ProductStatus.CHO_PHAN_KHO.ToString());

                            var pendingList = pendingProducts.ToList();
                            int count = pendingList.Count;
                            var now = DateTime.Now;

                            bool triggerAssign = false;

                            // ĐIỀU KIỆN 1: Vượt ngưỡng số lượng (vd: >= 200 đơn)
                            if (count >= settings.ImmediateThreshold)
                            {
                                triggerAssign = true;
                                Console.WriteLine($"[AUTO-ASSIGN] Vuot nguong {count} don");
                            }

                            // ĐIỀU KIỆN 2: Đến giờ chốt sổ 
                            if (TimeOnly.TryParse(settings.ScheduleTime, out var configTime))
                            {
                                if (now.Hour == configTime.Hour && now.Minute == configTime.Minute)
                                {
                                    if (count >= settings.ScheduleMinQty)
                                    {
                                        triggerAssign = true;
                                        Console.WriteLine($"[AUTO-ASSIGN] Chia {count} don vao luc {settings.ScheduleTime}.");
                                    }
                                }
                            }

                            if (triggerAssign && count > 0)
                            {
           
                                var adminUser = (await unitOfWork.Users.GetAllAsync(u =>
                                    u.Role == UserRole.Admin.ToString() &&
                                    u.Status == UserStatus.DANG_HOAT_DONG.ToString())).FirstOrDefault();

                                if (adminUser != null)
                                {
                                    var productIds = pendingList.Select(p => p.ProductId).ToList();

                                    assignService.AssignProductsInBackground(
                                        productIds,
                                        DateOnly.FromDateTime(now),
                                        adminUser.UserId.ToString()
                                    );

                                    Console.WriteLine($"[AUTO-ASSIGN SUCCESS]");
                                }
                                else
                                {
                                    Console.WriteLine("[AUTO-ASSIGN ERROR] Khong co admin");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AUTO-ASSIGN EXCEPTION]: {ex.Message}");
                    }
                }
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
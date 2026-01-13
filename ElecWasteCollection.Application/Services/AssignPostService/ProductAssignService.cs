using ElecWasteCollection.Application.IServices.IAssignPost;
using ElecWasteCollection.Application.Model.AssignPost;
using ElecWasteCollection.Application.Helpers;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
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

		// =========================================================================
		// PHẦN 1: HÀM PUBLIC - GỌI TỪ CONTROLLER (FIRE-AND-FORGET)
		// =========================================================================
		public void AssignProductsInBackground(List<Guid> productIds, DateOnly workDate, string userId)
		{
			// Chạy luồng riêng biệt (Background Thread)
			Task.Run(async () =>
			{
				// TẠO SCOPE MỚI: Quan trọng nhất để tránh lỗi ObjectDisposedException
				using (var scope = _scopeFactory.CreateScope())
				{
					// Resolve lại các service từ Scope mới này
					var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
					var distanceCache = scope.ServiceProvider.GetRequiredService<IMapboxDistanceCacheService>();
					var notifService = scope.ServiceProvider.GetRequiredService<IWebNotificationService>();

					try
					{
						// Gọi hàm logic tính toán nội bộ (xem Phần 2)
						var result = await AssignProductsLogicInternal(unitOfWork, distanceCache, productIds, workDate);

						var summaryData = new
						{
							Action = "ASSIGN_COMPLETED",
							TotalRequested = productIds.Count,
							Success = result.TotalAssigned,
							Failed = result.Details.Count(x => (string)x.GetType().GetProperty("status")?.GetValue(x, null)! == "failed"),
							Unassigned = result.TotalUnassigned
						};
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
		// PHẦN 2: LOGIC XỬ LÝ CHÍNH (ĐÃ TỐI ƯU BULK READ)
		// =========================================================================
		private async Task<AssignProductResult> AssignProductsLogicInternal(
			IUnitOfWork unitOfWork,
			IMapboxDistanceCacheService distanceCache,
			List<Guid> productIds,
			DateOnly workDate)
		{
			var result = new AssignProductResult();

			// 1. Bulk Read Config & Company
			var companies = await unitOfWork.Companies.GetAllAsync(includeProperties: "SmallCollectionPoints");
			if (!companies.Any()) throw new Exception("Lỗi cấu hình: Chưa có đơn vị thu gom nào trong hệ thống.");

			var allConfigs = await unitOfWork.SystemConfig.GetAllAsync();

			// Tính toán tỷ lệ Config
			var sortedConfig = companies.OrderBy(c => c.CompanyId).ToList();
			double totalPercent = sortedConfig.Sum(c => GetConfigValue(allConfigs, c.CompanyId, null, SystemConfigKey.ASSIGN_RATIO, 0));

			if (Math.Abs(totalPercent - 100) > 0.1)
				throw new Exception($"Lỗi cấu hình: Tổng tỉ lệ phân bổ là {totalPercent}%, yêu cầu là 100%.");

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

			// 2. Bulk Read Data (Lấy hết dữ liệu cần thiết 1 lần)
			var products = await unitOfWork.Products.GetAllAsync(filter: p => productIds.Contains(p.ProductId));
			if (!products.Any()) throw new Exception("Không tìm thấy sản phẩm nào hợp lệ.");

			// Lấy Post liên quan
			var postIds = products.Select(p => p.ProductId).ToList();
			var posts = await unitOfWork.Posts.GetAllAsync(p => postIds.Contains(p.ProductId));

			// Lấy Address liên quan
			var senderIds = posts.Select(p => p.SenderId).Distinct().ToList();
			var addresses = await unitOfWork.UserAddresses.GetAllAsync(a => senderIds.Contains(a.UserId));

			// List tạm để lưu History chuẩn bị Insert
			var historyListToAdd = new List<ProductStatusHistory>();

			// 3. Xử lý Logic (Chạy trên RAM -> Rất nhanh)
			foreach (var product in products)
			{
				try
				{
					// Lấy Post từ RAM
					var post = posts.FirstOrDefault(p => p.ProductId == product.ProductId);
					if (post == null)
					{
						result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Không tìm thấy bài đăng (Post)" });
						continue;
					}

					if (string.IsNullOrEmpty(post.Address))
					{
						result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Địa chỉ bài đăng bị trống" });
						continue;
					}

					// Lấy Address từ RAM
					var matchedAddress = addresses.FirstOrDefault(a => a.UserId == post.SenderId && a.Address == post.Address);
					if (matchedAddress == null || matchedAddress.Iat == null || matchedAddress.Ing == null)
					{
						result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Không lấy được tọa độ (Lat/Lng)" });
						continue;
					}

					// Tìm kho (Candidate)
					var validCandidates = new List<ProductAssignCandidate>();
					foreach (var company in sortedConfig)
					{
						// Truyền distanceCache vào để sử dụng
						var candidate = await FindBestSmallPointForCompanyAsync(company, matchedAddress, allConfigs, distanceCache);
						if (candidate != null) validCandidates.Add(candidate);
					}

					ProductAssignCandidate? chosenCandidate = null;
					string assignNote = "";

					if (!validCandidates.Any())
					{
						result.TotalUnassigned++;
						result.Details.Add(new { productId = product.ProductId, status = "failed", reason = "Không có đơn vị thu gom gần đây" });

						product.Status = ProductStatus.KHONG_TIM_THAY_DIEM_THU_GOM.ToString();
						// Không cần gọi _unitOfWork.Products.Update(product) vì EF Core tự track change
					}
					else
					{
						// Logic Hash chia việc công bằng
						double magicNumber = GetStableHashRatio(product.ProductId);
						var targetConfig = rangeConfigs.FirstOrDefault(t => magicNumber >= t.MinRange && magicNumber < t.MaxRange);
						if (targetConfig == null) targetConfig = rangeConfigs.Last();

						var targetCandidate = validCandidates.FirstOrDefault(c => c.CompanyId == targetConfig.CompanyEntity.CompanyId);

						if (targetCandidate != null)
						{
							chosenCandidate = targetCandidate;
							assignNote = $"Đúng tuyến - Tỉ lệ {targetConfig.AssignRatio}%";
						}
						else
						{
							chosenCandidate = validCandidates.OrderBy(c => c.RoadKm).First();
							assignNote = "Trái tuyến - Chọn kho gần nhất";
						}
					}

					if (chosenCandidate != null)
					{
						post.CollectionCompanyId = chosenCandidate.CompanyId;
						post.AssignedSmallPointId = chosenCandidate.SmallPointId;
						post.DistanceToPointKm = chosenCandidate.RoadKm;

						product.SmallCollectionPointId = chosenCandidate.SmallPointId;
						product.Status = ProductStatus.CHO_GOM_NHOM.ToString();

						// Thêm vào list tạm, chưa lưu vội
						historyListToAdd.Add(new ProductStatusHistory
						{
							ProductStatusHistoryId = Guid.NewGuid(),
							ProductId = product.ProductId,
							ChangedAt = DateTime.UtcNow,
							Status = ProductStatus.CHO_GOM_NHOM.ToString(),
							StatusDescription = $"Đã phân bổ về kho: {chosenCandidate.SmallPointId} - {assignNote}"
						});

						result.TotalAssigned++;
						result.Details.Add(new
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
					result.Details.Add(new { productId = product.ProductId, status = "error", message = $"Lỗi hệ thống: {ex.Message}" });
				}
			}

			// 4. Lưu DB 1 lần duy nhất (Batch Insert/Update)
			if (historyListToAdd.Any())
			{
				// Vì không có AddRange, ta dùng loop AddAsync
				foreach (var history in historyListToAdd)
				{
					await unitOfWork.ProductStatusHistory.AddAsync(history);
				}
			}

			// SaveAsync sẽ đẩy tất cả thay đổi của Product, Post và Insert History xuống DB trong 1 transaction
			await unitOfWork.SaveAsync();

			return result;
		}

		// =========================================================================
		// PHẦN 3: CÁC HÀM HELPER
		// =========================================================================

		private async Task<ProductAssignCandidate?> FindBestSmallPointForCompanyAsync(
			Company company,
			UserAddress address,
			IEnumerable<SystemConfig> configs,
			IMapboxDistanceCacheService distanceCache) // Nhận Cache từ tham số
		{
			ProductAssignCandidate? best = null;
			double minRoadKm = double.MaxValue;

			if (company.SmallCollectionPoints == null) return null;

			foreach (var sp in company.SmallCollectionPoints)
			{
				double radiusKm = GetConfigValue(configs, null, sp.SmallCollectionPointsId, SystemConfigKey.RADIUS_KM, 10);
				double maxRoadKm = GetConfigValue(configs, null, sp.SmallCollectionPointsId, SystemConfigKey.MAX_ROAD_DISTANCE_KM, 15);

				double hvDistance = GeoHelper.DistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
				if (hvDistance > radiusKm) continue;

				// Dùng cache được truyền vào
				double roadKm = await distanceCache.GetRoadDistanceKm(sp.Latitude, sp.Longitude, address.Iat ?? 0, address.Ing ?? 0);
				if (roadKm > maxRoadKm) continue;

				if (roadKm < minRoadKm)
				{
					minRoadKm = roadKm;
					best = new ProductAssignCandidate
					{
						ProductId = Guid.Empty,
						CompanyId = company.CompanyId,
						SmallPointId = sp.SmallCollectionPointsId,
						RoadKm = roadKm,
						HaversineKm = hvDistance
					};
				}
			}
			return best;
		}

		private double GetConfigValue(IEnumerable<SystemConfig> configs, string? companyId, string? pointId, SystemConfigKey key, double defaultValue)
		{
			var config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.SmallCollectionPointId == pointId && pointId != null);

			if (config == null && companyId != null)
				config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.CompanyId == companyId && x.SmallCollectionPointId == null);

			if (config == null)
				config = configs.FirstOrDefault(x => x.Key == key.ToString() && x.CompanyId == null && x.SmallCollectionPointId == null);

			if (config != null && double.TryParse(config.Value, out double result))
				return result;

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
				{
					if (DateOnly.TryParse(d.PickUpDate, out var date))
						dates.Add(date);
				}
				return dates.Any();
			}
			catch { return false; }
		}

		// =========================================================================
		// PHẦN 4: HÀM GET DỮ LIỆU (Dùng _unitOfWorkForGet inject từ constructor)
		// =========================================================================
		public async Task<List<ProductByDateModel>> GetProductsByWorkDateAsync(DateOnly workDate)
		{
			// Hàm này chỉ đọc dữ liệu hiển thị lên UI, nên dùng UnitOfWork thường là được
			var posts = await _unitOfWorkForGet.Posts.GetAllAsync(
				filter: p => p.Product != null && p.Product.Status == ProductStatus.CHO_PHAN_KHO.ToString(),
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
					CategoryName = post.Product.Category?.Name ?? "Không xác định",
					BrandName = post.Product.Brand?.Name ?? "Không xác định",
					UserName = post.Sender?.Name ?? "Không xác định",
					Address = displayAddr
				});
			}

			return result;
		}

		// =========================================================================
		// Private DTOs
		// =========================================================================
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
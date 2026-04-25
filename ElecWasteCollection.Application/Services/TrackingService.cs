using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.Helper;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class TrackingService : ITrackingService
	{
		private readonly ITrackingRepository _trackingRepository;
		private readonly IProductRepository _productRepository;
		private readonly IPostRepository _postRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IRedisCacheService _redisCacheService;

		public TrackingService(ITrackingRepository trackingRepository, IProductRepository productRepository, IPostRepository postRepository, IUnitOfWork unitOfWork, IRedisCacheService redisCacheService)
		{
			_trackingRepository = trackingRepository;
			_productRepository = productRepository;
			_postRepository = postRepository;
			_unitOfWork = unitOfWork;
			_redisCacheService = redisCacheService;
		}

		public async Task<ProductTrackingTimelineResponse> GetFullTimelineByProductIdAsync(Guid id)
		{
			var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			// 1. TRA CỨU ĐA TẦNG (SQL -> Redis Map)
			// Thử tìm Post bằng PostId hoặc ProductId (đã có trong SQL)
			var post = await _unitOfWork.Posts.GetAsync(
				p => p.PostId == id || (p.Product != null && p.Product.ProductId == id),
				includeProperties: "Sender,Images,Product,Product.Category,Product.Brand"
			);

			// Fallback Redis: Nếu id truyền vào là ProductId nháp (trường hợp bài chưa được duyệt)
			if (post == null)
			{
				var mappedPostIdStr = await _redisCacheService.GetStringAsync($"ewise:product_map:{id}");
				if (!string.IsNullOrEmpty(mappedPostIdStr) && Guid.TryParse(mappedPostIdStr, out var mappedPostId))
				{
					post = await _unitOfWork.Posts.GetAsync(
						p => p.PostId == mappedPostId,
						includeProperties: "Sender,Images,Product,Product.Category,Product.Brand"
					);
				}
			}

			if (post == null) throw new AppException("Không tìm thấy thông tin sản phẩm để theo dõi", 404);

			// 2. CẤU HÌNH MÚI GIỜ & KHỞI TẠO
			string timeZoneId = "SE Asia Standard Time";
			TimeZoneInfo vnTimeZone;
			try { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId); }
			catch { vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh"); }

			var timelineResponse = new List<CollectionTimelineModel>();
			ProductDetailForTracking productResponse;
			ProductDraftModel? draftData = null;

			if (post.Product != null)
			{
				var product = post.Product;
				var pointTransactions = await _unitOfWork.PointTransactions.GetsAsync(t => t.ProductId == product.ProductId);
				double? realPoints = pointTransactions?.Any() == true ? pointTransactions.Sum(t => t.Point) : post.EstimatePoint;

				productResponse = new ProductDetailForTracking
				{
					CategoryName = product.Category?.Name ?? "Không rõ", // Cate con từ DB
					Description = product.Description,
					BrandName = product.Brand?.Name ?? "Không rõ",
					Images = post.Images?.Select(img => img.ImageUrl).ToList()
							 ?? product.Images?.Select(img => img.ImageUrl).ToList()
							 ?? new List<string>(),
					Address = post.Address,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status),
					Points = realPoints,
					CollectionRouteId = product.CollectionRoutes?.OrderByDescending(r => r.CollectionDate).FirstOrDefault()?.CollectionRouteId ?? Guid.Empty
				};

				var history = await _unitOfWork.ProductStatusHistory.GetsAsync(h => h.ProductId == product.ProductId);
				if (history != null && history.Any())
				{
					foreach (var h in history)
					{
						timelineResponse.Add(MapToTimelineModel(h.Status, h.StatusDescription, h.ChangedAt, vnTimeZone));
					}
				}
			}
			else
			{
				string? draftJson = null;
				if (post.Status == PostStatus.CHO_DUYET.ToString())
					draftJson = await _redisCacheService.GetStringAsync($"ewise:draft_product:{post.PostId}");
				else if (post.CheckMessage != null)
				{
					var trick = post.CheckMessage.FirstOrDefault(x => x.StartsWith("[REJECTED_PRODUCT_DATA]"));
					if (trick != null) draftJson = trick.Split('|')[1];
				}

				draftData = !string.IsNullOrEmpty(draftJson) ? JsonSerializer.Deserialize<ProductDraftModel>(draftJson, jsonOptions) : null;

				productResponse = new ProductDetailForTracking
				{
					CategoryName = draftData?.ChildCategoryName ?? "Dữ liệu đang chờ", // Cate con từ Draft
					Description = post.Description ?? draftData?.Product?.Description ?? "Không có mô tả",
					BrandName = draftData?.BrandName ?? "Không rõ",
					Images = post.Images?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),
					Address = post.Address,
					Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status),
					Points = post.EstimatePoint,
					CollectionRouteId = Guid.Empty
				};

				// Lấy mốc History "Lúc tạo" từ Redis
				if (draftData?.History != null)
				{
					timelineResponse.Add(MapToTimelineModel(
						draftData.History.Status,
						draftData.History.StatusDescription,
						draftData.History.ChangedAt,
						vnTimeZone));
				}
			}

			var currentStatusName = StatusEnumHelper.ConvertDbCodeToVietnameseName<PostStatus>(post.Status);
			if (!timelineResponse.Any(t => t.Status == currentStatusName))
			{
				timelineResponse.Add(MapToTimelineModel(
					post.Status,
					(post.Status == PostStatus.DA_HUY.ToString() || post.Status == PostStatus.DA_TU_CHOI.ToString())
						? (post.RejectMessage ?? "Yêu cầu đã được đóng")
						: "Yêu cầu đã được ghi nhận",
					post.Date,
					vnTimeZone));
			}

			// 5. TRẢ VỀ KẾT QUẢ SẮP XẾP MỚI NHẤT TRÊN ĐẦU
			return new ProductTrackingTimelineResponse
			{
				ProductInfo = productResponse,
				Timeline = timelineResponse.OrderByDescending(t => DateTime.ParseExact($"{t.Date} {t.Time}", "dd/MM/yyyy HH:mm", null)).ToList()
			};
		}

		// Hàm phụ để Map Timeline tránh lặp code
		private CollectionTimelineModel MapToTimelineModel(string status, string description, DateTime dateTime, TimeZoneInfo tz)
		{
			var utcTime = dateTime.Kind == DateTimeKind.Utc ? dateTime : DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
			var vnTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);

			return new CollectionTimelineModel
			{
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(status),
				Description = description,
				Date = vnTime.ToString("dd/MM/yyyy"),
				Time = vnTime.ToString("HH:mm")
			};
		}


	}
}
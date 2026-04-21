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
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class TrackingService : ITrackingService
	{
		private readonly ITrackingRepository _trackingRepository;
		private readonly IProductRepository _productRepository;
		private readonly IPostRepository _postRepository;
		private readonly IUnitOfWork _unitOfWork;

		public TrackingService(ITrackingRepository trackingRepository, IProductRepository productRepository, IPostRepository postRepository, IUnitOfWork unitOfWork)
		{
			_trackingRepository = trackingRepository;
			_productRepository = productRepository;
			_postRepository = postRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<ProductTrackingTimelineResponse> GetFullTimelineByProductIdAsync(Guid productId)
		{
			var timeline = await _trackingRepository.GetsAsync(h => h.ProductId == productId);
			var product = await _productRepository.GetProductWithDetailsAsync(productId);

			if (product == null)
			{
				throw new AppException("Không tìm thấy sản phẩm", 404);
			}

			var post = await _postRepository.GetAsync(p => p.Product.ProductId == productId);

			var pointTransactions = await _unitOfWork.PointTransactions.GetsAsync(t => t.ProductId == productId);

			double? realPoints = null;
			if (pointTransactions != null && pointTransactions.Any())
			{
				realPoints = pointTransactions.Sum(t => t.Point);
			}
			else if (post != null)
			{
				realPoints = post.EstimatePoint;
			}

			string timeZoneId = "SE Asia Standard Time";
			TimeZoneInfo vnTimeZone;
			try
			{
				vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
			}
			catch (TimeZoneNotFoundException)
			{
				vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
			}

			var productResponse = new ProductDetailForTracking
			{
				CategoryName = product.Category.Name,
				Description = product.Description,
				BrandName = product.Brand.Name,
				Images = product.ProductImages.Select(img => img.ImageUrl).ToList(),
				Address = post != null ? post.Address : "Không có địa chỉ thu gom do người dùng tự mang đến kho",
				Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(product.Status).ToString(),
				Points = realPoints ,
				CollectionRouteId = product.CollectionRoutes != null && product.CollectionRoutes.Any()
							? product.CollectionRoutes.OrderByDescending(r => r.CollectionDate).FirstOrDefault().CollectionRouteId
							: Guid.Empty
			};

			// 2. Chuẩn bị danh sách Timeline
			var timelineResponse = new List<CollectionTimelineModel>();
			if (timeline != null && timeline.Any())
			{
				timelineResponse = timeline.OrderByDescending(h => h.ChangedAt).Select(h =>
				{
					var utcTime = h.ChangedAt.Kind == DateTimeKind.Utc
								? h.ChangedAt
								: DateTime.SpecifyKind(h.ChangedAt, DateTimeKind.Utc);

					var vnTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, vnTimeZone);

					return new CollectionTimelineModel
					{
						Status = StatusEnumHelper.ConvertDbCodeToVietnameseName<ProductStatus>(h.Status).ToString(),
						Description = h.StatusDescription,
						Date = vnTime.ToString("dd/MM/yyyy"),
						Time = vnTime.ToString("HH:mm")
					};
				}).ToList();
			}

			// 3. Trả về đối tượng tổng hợp
			return new ProductTrackingTimelineResponse
			{
				ProductInfo = productResponse,
				Timeline = timelineResponse
			};
		}


	}
}
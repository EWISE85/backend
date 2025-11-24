using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class TrackingService : ITrackingService
	{
		// Lấy TẤT CẢ các danh sách cần thiết từ FakeDataSeeder
		private readonly List<Post> _posts = FakeDataSeeder.posts;
		private readonly List<Products> _products = FakeDataSeeder.products;
		private readonly List<CollectionRoutes> _routes = FakeDataSeeder.collectionRoutes;
		private readonly List<ProductStatusHistory> _productStatusHistories = FakeDataSeeder.productStatusHistories;


		/// <summary>
		/// Lấy timeline của MỘT CHUYẾN ĐI (route) cụ thể
		/// </summary>
		//public Task<List<CollectionTimelineModel>> GetCollectionTimelineAsync(Guid collectionRouteId)
		//{
		//	var timelineEvents = new List<CollectionTimelineModel>();

		//	// 1. Tìm chuyến đi (route)
		//	var route = _routes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
		//	if (route == null)
		//	{
		//		return Task.FromResult(timelineEvents); // Không tìm thấy route, trả về list rỗng
		//	}

		//	// 2. Tìm bài đăng (post) gốc của chuyến đi đó
		//	var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
		//	if (post == null)
		//	{
		//		return Task.FromResult(timelineEvents); // Lỗi dữ liệu, trả về list rỗng
		//	}

		//	// 3. Thêm sự kiện "Gửi yêu cầu" (từ bảng Post)
		//	timelineEvents.Add(new CollectionTimelineModel
		//	{
		//		Status = "Đã gửi",
		//		Description = "Yêu cầu đã được gửi đi.",
		//		Timestamp = post.Date
		//	});

		//	// 4. Thêm sự kiện "Duyệt / Từ chối" (từ bảng Post)
		//	if (post.Status != "Chờ duyệt") // "Đã Duyệt" hoặc "Rejected"
		//	{
		//		timelineEvents.Add(new CollectionTimelineModel
		//		{
		//			Status = post.Status,
		//			Description = post.Status == "Rejected" ? post.RejectMessage : "Yêu cầu đã được duyệt.",
		//			// Do Fake data không có ngày duyệt, ta giả lập là 5 phút sau khi tạo
		//			Timestamp = post.Date.AddMinutes(5)
		//		});
		//	}

		//	// 5. Lấy tất cả lịch sử của CHUYẾN ĐI (ROUTE) này
		//	var routeHistories = _collectionRouteStatusHistories
		//		.Where(h => h.CollectionRouteId == collectionRouteId)
		//		.Select(h => new CollectionTimelineModel
		//		{
		//			Status = h.Status,
		//			Description = h.StatusDescription,
		//			Timestamp = h.ChangedAt
		//		});

		//	timelineEvents.AddRange(routeHistories);

		//	// 6. Sắp xếp tất cả sự kiện theo thời gian và trả về
		//	var sortedTimeline = timelineEvents.OrderBy(e => e.Timestamp).ToList();

		//	// Dùng Task.FromResult vì ta đang dùng fake list (đồng bộ)
		//	// nhưng vẫn tuân thủ interface (bất đồng bộ)
		//	return Task.FromResult(sortedTimeline);
		//}

		public Task<List<CollectionTimelineModel>> GetFullTimelineByProductIdAsync(Guid productId)
		{
			

			
			var timeline = _productStatusHistories
				.Where(h => h.ProductId == productId) // Lọc theo ProductId
				.OrderBy(h => h.ChangedAt)            // Sắp xếp theo thời gian
				.Select(h => new CollectionTimelineModel // Map sang model trả về
				{
					Status = h.Status,
					Description = h.StatusDescription,
					Date = h.ChangedAt.ToString("dd/MM/yyyy"), // Format
					Time = h.ChangedAt.ToString("HH:mm")       // Format
				})
				.ToList();

			return Task.FromResult(timeline);
		}

		/// <summary>
		/// Lấy lịch sử của MỘT SẢN PHẨM (product) cụ thể
		/// </summary>
		//public Task<List<ProductHistoryModel>> GetProductHistoryAsync(Guid productId)
		//{
		//	// 1. Kiểm tra sản phẩm có tồn tại không
		//	if (!_products.Any(p => p.Id == productId))
		//	{
		//		// Trả về list rỗng
		//		return Task.FromResult(new List<ProductHistoryModel>());
		//	}

		//	// 2. Lấy lịch sử, sắp xếp, và map sang Model
		//	var history = _productStatusHistories
		//		.Where(h => h.ProductId == productId)
		//		.OrderBy(h => h.ChangedAt) // Sắp xếp theo thời gian
		//		.Select(h => new ProductHistoryModel
		//		{
		//			Status = h.Status,
		//			Description = h.StatusDescription,
		//			Timestamp = h.ChangedAt
		//		})
		//		.ToList();

		//	return Task.FromResult(history);
		//}
	}
}
using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class CollectionRouteService : ICollectionRouteService
	{
		// === SỬA LỖI: Tiêm (Inject) TẤT CẢ các List<> cần thiết từ FakeDataSeeder ===
		// Chúng ta sẽ dùng List<> thay vì các Service khác để join

		private readonly List<CollectionRoutes> _collectionRoutes = FakeDataSeeder.collectionRoutes;
		private readonly List<Collector> _collectors = FakeDataSeeder.collectors;
		private readonly List<Shifts> _shifts = FakeDataSeeder.shifts;
		private readonly List<CollectionGroups> _collectionGroups = FakeDataSeeder.collectionGroups;
		private readonly List<Post> _posts = FakeDataSeeder.posts;
		private readonly List<User> _users = FakeDataSeeder.users;
		private readonly List<Products> _products = FakeDataSeeder.products; // Sửa: Dùng List, không dùng IProductService
		private readonly List<PostImages> _postImages = FakeDataSeeder.postImages;
		private readonly List<Vehicles> _vehicles = FakeDataSeeder.vehicles;
		private readonly IShippingNotifierService _notifierService; // Dịch vụ này vẫn giữ lại

		public CollectionRouteService(IShippingNotifierService notifierService)
		{
			_notifierService = notifierService;
		}

		public bool CancelCollection(Guid collectionRouteId, string rejectMessage)
		{
			// SỬA LỖI: Dùng _collectionRoutes
			var route = _collectionRoutes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
			if (post == null) return false;
			var product = _products.FirstOrDefault(p => p.Id == post.ProductId);
			if (product == null) return false;

			if (route != null)
			{
				route.Status = "Hủy bỏ";
				route.RejectMessage = rejectMessage;
				product.Status = "Hủy bỏ";
				return true;
			}
			return false;
		}

		public bool ConfirmCollection(Guid collectionRouteId, List<string> confirmImages, string QRCode)
		{
			// SỬA LỖI: Dùng _collectionRoutes
			var route = _collectionRoutes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			if (route == null)
			{
				return false;
			}

			// SỬA LỖI: Dùng _posts (List) thay vì _postService
			var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
			if (post == null) return false;

			// SỬA LỖI: Dùng _products (List) và post.ProductId
			var product = _products.FirstOrDefault(p => p.Id == post.ProductId);
			if (product == null) return false;

			route.Status = "Hoàn thành";
			route.ConfirmImages = confirmImages;
			route.Actual_Time = TimeOnly.FromDateTime(DateTime.Now);
			product.QRCode = QRCode;
			product.Status = "Đã thu gom";
			return true;
		}

		// === HÀM NÀY ĐÃ VIẾT LẠI HOÀN TOÀN ===
		public List<CollectionRouteModel> GetAllRoutes(DateOnly PickUpDate)
		{
			var routes = _collectionRoutes
				.Where(r => r.CollectionDate == PickUpDate);

			var results = routes.Select(r =>
			{
				// Dùng hàm helper mới để join dữ liệu
				return BuildCollectionRouteModel(r);
			})
			.Where(model => model != null) // Lọc bỏ data lỗi
			.OrderBy(r => r.EstimatedTime)
			.ToList();

			return results;
		}

		public List<CollectionRouteModel> GetAllRoutesByDateAndByCollectionPoints(DateOnly PickUpDate, int collectionPointId)
		{
			// 1. Tìm các xe thuộc điểm thu gom
			var vehicleIds = _vehicles
				.Where(v => v.Small_Collection_Point == collectionPointId)
				.Select(v => v.Id)
				.ToList();

			// 2. Tìm các ca làm việc VÀO NGÀY ĐÓ dùng các xe đó
			var shiftIds = _shifts
				.Where(s => vehicleIds.Contains(s.Vehicle_Id) && s.WorkDate == PickUpDate)
				.Select(s => s.Id)
				.ToList();

			if (!shiftIds.Any())
			{
				// Điểm thu gom này không có ca làm việc nào vào ngày này
				return new List<CollectionRouteModel>();
			}

			// 3. Tìm các nhóm thuộc các ca làm việc đó
			var groupIds = _collectionGroups
				.Where(g => shiftIds.Contains(g.Shift_Id))
				.Select(g => g.Id)
				.ToList();

			// 4. Tìm các tuyến đường thuộc các nhóm đó
			// (Thêm 1 lần lọc PickUpDate nữa cho chắc)
			var routes = _collectionRoutes
				.Where(r => groupIds.Contains(r.CollectionGroupId) && r.CollectionDate == PickUpDate);

			// 5. Build model
			var results = routes.Select(r =>
			{
				// Dùng hàm helper (phiên bản đầy đủ)
				return BuildCollectionRouteModel(r);
			})
			.Where(model => model != null) // Lọc bỏ data lỗi
			.OrderBy(model => model.EstimatedTime) // Sắp xếp
			.ToList();

			return results;
		}

		// === HÀM NÀY ĐÃ VIẾT LẠI HOÀN TOÀN ===
		public CollectionRouteModel GetRouteById(Guid collectionRouteId)
		{
			// SỬA LỖI: Dùng _collectionRoutes
			var route = _collectionRoutes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			if (route == null)
			{
				return null;
			}

			// Dùng hàm helper mới để join dữ liệu
			return BuildCollectionRouteModel(route);
		}

		// === HÀM NÀY ĐÃ SỬA LẠI CÁC LỖI TYPO ===
		public List<CollectionRouteModel> GetRoutesByCollectorId(DateOnly PickUpDate, Guid collectorId)
		{
			var collector = _collectors.FirstOrDefault(c => c.CollectorId == collectorId);
			if (collector == null)
			{
				return new List<CollectionRouteModel>();
			}

			var shiftIds = _shifts
				.Where(s => s.CollectorId == collectorId && s.WorkDate == PickUpDate)
				.Select(s => s.Id)
				.ToList();

			if (!shiftIds.Any())
			{
				return new List<CollectionRouteModel>();
			}

			var groupIds = _collectionGroups
				.Where(g => shiftIds.Contains(g.Shift_Id))
				.Select(g => g.Id)
				.ToList();

			var routes = _collectionRoutes
				.Where(r => groupIds.Contains(r.CollectionGroupId) && r.CollectionDate == PickUpDate);

			var results = routes.Select(r =>
			{
				var post = _posts.FirstOrDefault(p => p.Id == r.PostId);
				if (post == null) return null;

				// SỬA LỖI: Dùng post.SenderId (thay vì post.S)
				var sender = _users.FirstOrDefault(u => u.UserId == post.SenderId);
				if (sender == null) return null;

				var pickUpImages = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();

				var group = _collectionGroups.FirstOrDefault(g => g.Id == r.CollectionGroupId);
				var shift = _shifts.FirstOrDefault(s => s.Id == group.Shift_Id);
				var vehicle = _vehicles.FirstOrDefault(v => v.Id == shift.Vehicle_Id);

				return new CollectionRouteModel
				{
					CollectionRouteId = r.CollectionRouteId,
					PostId = r.PostId,
					ItemName = post.Name,
					Collector = collector,
					Sender = sender,
					CollectionDate = r.CollectionDate,
					EstimatedTime = r.EstimatedTime,
					Actual_Time = r.Actual_Time,
					ConfirmImages = r.ConfirmImages,
					PickUpItemImages = pickUpImages,
					LicensePlate = vehicle?.Plate_Number ?? "N/A",
					Address = post.Address,
					Status = r.Status
				};
			})
			.Where(model => model != null)
			.OrderBy(model => model.EstimatedTime)
			.ToList();

			return results;
		}

		// === HÀM NÀY ĐÃ SỬA LỖI LOGIC ===
		public async Task<bool> IsUserConfirm(Guid collectionRouteId, bool isConfirm, bool isSkip)
		{
			// SỬA LỖI: Dùng _collectionRoutes
			var route = _collectionRoutes.FirstOrDefault(r => r.CollectionRouteId == collectionRouteId);
			if (route != null)
			{
				if (isSkip)
				{
					route.Status = "User_Skip";
					return true;
				}
				if (isConfirm)
				{
					route.Status = "User_Confirm";
				}
				else
				{
					route.Status = "User_Reject";
				}

				// SỬA LỖI: Phải join để tìm CollectorId
				var group = _collectionGroups.FirstOrDefault(g => g.Id == route.CollectionGroupId);
				if (group == null) return false; // Lỗi data

				var shift = _shifts.FirstOrDefault(s => s.Id == group.Shift_Id);
				if (shift == null) return false; // Lỗi data

				// Đã tìm thấy CollectorId
				Guid collectorId = shift.CollectorId;

				await _notifierService.NotifyShipperOfConfirmation(
					collectorId.ToString(), // Sửa: Dùng collectorId vừa tìm được
					collectionRouteId,
					route.Status);

				return true;
			}

			return false;
		}

		// === HÀM HELPER MỚI (để GetAllRoutes và GetRouteById sử dụng) ===
		private CollectionRouteModel BuildCollectionRouteModel(CollectionRoutes route)
		{
			try
			{
				var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
				if (post == null) return null;

				var sender = _users.FirstOrDefault(u => u.UserId == post.SenderId);
				if (sender == null) return null;

				var pickUpImages = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();

				// Join để lấy Collector và Vehicle
				var group = _collectionGroups.FirstOrDefault(g => g.Id == route.CollectionGroupId);
				if (group == null) return null;

				var shift = _shifts.FirstOrDefault(s => s.Id == group.Shift_Id);
				if (shift == null) return null;

				var collector = _collectors.FirstOrDefault(c => c.CollectorId == shift.CollectorId);
				if (collector == null) return null;

				var vehicle = _vehicles.FirstOrDefault(v => v.Id == shift.Vehicle_Id);
				if (vehicle == null) return null;

				return new CollectionRouteModel
				{
					CollectionRouteId = route.CollectionRouteId,
					PostId = route.PostId,
					ItemName = post.Name,
					Collector = collector,
					Sender = sender,
					CollectionDate = route.CollectionDate,
					EstimatedTime = route.EstimatedTime,
					Actual_Time = route.Actual_Time,
					ConfirmImages = route.ConfirmImages,
					PickUpItemImages = pickUpImages,
					LicensePlate = vehicle.Plate_Number,
					Address = post.Address,
					Status = route.Status
				};
			}
			catch (Exception)
			{
				// Bắt lỗi nếu data fake bị hỏng (ví dụ: null reference)
				return null;
			}
		}
		public PagedResult<CollectionRouteModel> GetPagedRoutes(RouteSearchQueryModel parameters)
		{
			// Bắt đầu với tất cả các tuyến
			var query = _collectionRoutes.AsQueryable();

			// 1. Lọc theo Trạm thu gom (CollectionPointId)
			if (parameters.CollectionPointId.HasValue)
			{
				// Join phức tạp: Route -> Group -> Shift -> Vehicle -> Point
				var vehicleIds = _vehicles
					.Where(v => v.Small_Collection_Point == parameters.CollectionPointId.Value)
					.Select(v => v.Id)
					.ToList();

				var shiftIds = _shifts
					.Where(s => vehicleIds.Contains(s.Vehicle_Id))
					.Select(s => s.Id)
					.ToList();

				var allowedGroupIds = _collectionGroups
					.Where(g => shiftIds.Contains(g.Shift_Id))
					.Select(g => g.Id)
					.ToHashSet(); // Dùng HashSet để tăng tốc độ lọc

				query = query.Where(r => allowedGroupIds.Contains(r.CollectionGroupId));
			}

			// 2. Lọc theo Ngày (PickUpDate)
			if (parameters.PickUpDate.HasValue)
			{
				query = query.Where(r => r.CollectionDate == parameters.PickUpDate.Value);
			}

			// 3. Lọc theo Trạng thái (Status)
			if (!string.IsNullOrEmpty(parameters.Status))
			{
				query = query.Where(r => r.Status.Equals(parameters.Status, StringComparison.OrdinalIgnoreCase));
			}

			// 4. Sắp xếp (Mặc định theo thời gian dự kiến)
			var sortedQuery = query
				.OrderBy(r => r.CollectionDate)
				.ThenBy(r => r.EstimatedTime);

			// 5. Lấy tổng số lượng (trước khi phân trang)
			int totalItems = sortedQuery.Count();

			// 6. Phân trang
			int page = parameters.Page <= 0 ? 1 : parameters.Page;
			int limit = parameters.Limit <= 0 ? 10 : parameters.Limit;

			var pagedData = sortedQuery
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList(); // Lấy ra danh sách CollectionRoutes

			// 7. Chuyển đổi (Select) sang Model đầy đủ
			var resultModels = pagedData
				.Select(route => BuildCollectionRouteModel(route)) // Dùng helper
				.Where(model => model != null)
				.ToList();

			// 8. Trả về kết quả phân trang
			return new PagedResult<CollectionRouteModel>
			{
				Data = resultModels,
				Page = page,
				Limit = limit,
				TotalItems = totalItems
			};
		}
	}
}
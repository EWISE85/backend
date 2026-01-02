using ElecWasteCollection.Application.Exceptions;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using ElecWasteCollection.Domain.IRepository;


namespace ElecWasteCollection.Application.Services
{
	public class CollectionRouteService : ICollectionRouteService
	{
		private readonly IShippingNotifierService _notifierService;
		private readonly ICollectionRouteRepository _collectionRouteRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IProductStatusHistoryRepository _productStatusHistoryRepository;
		private readonly IUserAddressRepository _userAddressRepository;


		public CollectionRouteService(IShippingNotifierService notifierService, ICollectionRouteRepository collectionRouteRepository, IUnitOfWork unitOfWork, IProductStatusHistoryRepository productStatusHistoryRepository, IUserAddressRepository userAddressRepository)
		{
			_notifierService = notifierService;
			_collectionRouteRepository = collectionRouteRepository;
			_unitOfWork = unitOfWork;
			_productStatusHistoryRepository = productStatusHistoryRepository;
			_userAddressRepository = userAddressRepository;
		}

		public async Task<bool> CancelCollection(Guid collectionRouteId, string rejectMessage)
		{
			var route = await _collectionRouteRepository.GetAsync(r => r.CollectionRouteId == collectionRouteId,includeProperties: "Product");

			if (route == null) throw new AppException("Không tìm thấy tuyến thu gom", 404);

			route.Status = "Hủy bỏ";
			route.RejectMessage = rejectMessage;

			if (route.Product != null)
			{
				route.Product.Status = "Hủy bỏ";

				var history = new ProductStatusHistory
				{
					ProductStatusHistoryId = Guid.NewGuid(), 
					ProductId = route.Product.ProductId,
					ChangedAt = DateTime.UtcNow, 
					Status = "Hủy bỏ",
					StatusDescription = $"Hủy thu gom: {rejectMessage}"
				};

				await _productStatusHistoryRepository.AddAsync(history);
				_unitOfWork.Products.Update(route.Product);
			}
			_unitOfWork.CollecctionRoutes.Update(route);
			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<bool> ConfirmCollection(Guid collectionRouteId, List<string> confirmImages, string QRCode)
		{
			
			var route = await _collectionRouteRepository.GetAsync(
				r => r.CollectionRouteId == collectionRouteId,
				includeProperties: "Product"
			);

			if (route == null) throw new AppException("Không tìm thấy tuyến thu gom", 404);


			route.Status = "Hoàn thành";
			route.ConfirmImages = confirmImages;
			route.Actual_Time = TimeOnly.FromDateTime(DateTime.UtcNow);

			if (route.Product != null)
			{
				route.Product.QRCode = QRCode;
				route.Product.Status = "Đã thu gom";
				var checkExistQrCode = await _unitOfWork.Products.GetAsync(p => p.QRCode == QRCode && p.ProductId != route.Product.ProductId);
				if (checkExistQrCode != null)
				{
					throw new AppException("Mã QR đã tồn tại trên hệ thống. Vui lòng kiểm tra lại.", 400);
				}
				var history = new ProductStatusHistory
				{
					ProductStatusHistoryId = Guid.NewGuid(), 
					ProductId = route.Product.ProductId,
					ChangedAt = DateTime.UtcNow, 
					StatusDescription = "Sản phẩm đã được thu gom thành công",
					Status = "Đã thu gom"
				};

				await _unitOfWork.ProductStatusHistory.AddAsync(history);
				_unitOfWork.Products.Update(route.Product);
			}
			_unitOfWork.CollecctionRoutes.Update(route);
			await _unitOfWork.SaveAsync();

			return true;
		}

		public async Task<List<CollectionRouteModel>> GetAllRoutes(DateOnly PickUpDate)
		{
			var routes = await _collectionRouteRepository.GetRoutesByDateWithDetailsAsync(PickUpDate);

			var results = new List<CollectionRouteModel>();

			foreach (var r in routes)
			{
				// Await từng cái một -> Tránh lỗi "A second operation..."
				var model = await BuildCollectionRouteModel(r);

				if (model != null)
				{
					results.Add(model);
				}
			}

			return results;
		}

		public async Task<List<CollectionRouteModel>> GetAllRoutesByDateAndByCollectionPoints(DateOnly PickUpDate, string collectionPointId)
		{
			var routes = await _collectionRouteRepository.GetRoutesByDateAndPointWithDetailsAsync(
				pickUpDate: PickUpDate,
				collectionPointId: collectionPointId
			);

			// SỬA: Dùng vòng lặp foreach để chạy tuần tự và lấy kết quả thực
			var results = new List<CollectionRouteModel>();

			foreach (var r in routes)
			{
				// Await từng cái một -> Tránh lỗi "A second operation..."
				var model = await BuildCollectionRouteModel(r);

				if (model != null)
				{
					results.Add(model);
				}
			}

			return results;
		}

		public async Task<CollectionRouteModel> GetRouteById(Guid collectionRoute)
		{
			var route = await _collectionRouteRepository.GetRouteWithDetailsByIdAsync(collectionRoute);

			if (route == null) throw new AppException("Không tìm thấy tuyến thu gom", 404);
			

			return await BuildCollectionRouteModel(route);
		}

		public async Task<List<CollectionRouteModel>> GetRoutesByCollectorId(DateOnly PickUpDate, Guid collectorId)
		{
			var routes = await _collectionRouteRepository.GetRoutesByCollectorAndDateWithDetailsAsync(
				pickUpDate: PickUpDate,
				collectorId: collectorId
			);

			if (routes == null || !routes.Any())
			{
				return new List<CollectionRouteModel>();
			}

			var results = new List<CollectionRouteModel>();

			foreach (var r in routes)
			{
				// Await từng cái một -> Tránh lỗi "A second operation..."
				var model = await BuildCollectionRouteModel(r);

				if (model != null)
				{
					results.Add(model);
				}
			}

			return results;
		}

		public async Task<bool> IsUserConfirm(Guid collectionRouteId, bool isConfirm, bool isSkip)
		{
			const string includeProps = "CollectionGroup.Shifts";
			var route = await _collectionRouteRepository.GetAsync(
				r => r.CollectionRouteId == collectionRouteId,
				includeProperties: includeProps
			);
			if (route == null) return false;
			var shifts = route.CollectionGroup?.Shifts;
			if (shifts == null) return false;

			Guid collectorId = shifts.CollectorId;
			string newStatus;
			if (isSkip)
			{
				newStatus = "User_Skip";
			}
			else if (isConfirm)
			{
				newStatus = "User_Confirm";
			}
			else
			{
				newStatus = "User_Reject";
			}
			route.Status = newStatus;
			_unitOfWork.CollecctionRoutes.Update(route);
			await _unitOfWork.SaveAsync();

			try
			{
				await _notifierService.NotifyShipperOfConfirmation(
					collectorId.ToString(),
					collectionRouteId,
					newStatus);

				return true;
			}
			catch (Exception ex)
			{
				

				Console.WriteLine($"Error notifying shipper: {ex.Message}");
				return true;
			}
		}

		private async Task<CollectionRouteModel> BuildCollectionRouteModel(CollectionRoutes route)
		{
			if (route == null) return null;

			var product = route.Product;
			
			var senderUser = product?.User;

			
			var relatedPost = senderUser?.Posts?
				.FirstOrDefault(p => p.ProductId == route.ProductId);

			var shifts = route.CollectionGroup?.Shifts;
			var collectorUser = shifts?.Collector;
			var vehicle = shifts?.Vehicle;

			UserResponse senderModel = null;
			if (senderUser != null)
			{
				senderModel = new UserResponse
				{
					UserId = senderUser.UserId,
					Name = senderUser.Name,
					Email = senderUser.Email,
					Phone = senderUser.Phone,
					Avatar = senderUser.Avatar,
					Role = senderUser.Role
				};
			}

			// 2. Map Collector Info
			CollectorResponse collectorModel = null;
			if (collectorUser != null)
			{
				collectorModel = new CollectorResponse
				{
					CollectorId = collectorUser.UserId,
					Name = collectorUser.Name,
					Email = collectorUser.Email,
					Phone = collectorUser.Phone,
					Avatar = collectorUser.Avatar,
					SmallCollectionPointId = collectorUser.SmallCollectionPointId
				};
			}

			// 3. Map Hình ảnh sản phẩm
			var productImages = new List<string>();
			if (product?.ProductImages != null)
			{
				productImages = product.ProductImages.Select(x => x.ImageUrl).ToList();
			}

			// 4. Xử lý địa chỉ
			// Nếu tìm thấy Post thì lấy Address của Post, nếu không thì báo N/A
			string address = relatedPost?.Address ?? "Không tìm thấy địa chỉ";
			var userAddress = await _userAddressRepository.GetAsync(ua => ua.UserId == senderUser.UserId && ua.Address == address); 
			//if (userAddress == null)
			//{
			//	throw new AppException("Không tìm thấy địa chỉ người dùng", 404);
			//}
			// 5. Trả về Model
			return new CollectionRouteModel
			{
				CollectionRouteId = route.CollectionRouteId,

				// Post Info (Lấy từ Post tìm được)
				PostId = relatedPost?.PostId ?? Guid.Empty,

				// Product Info
				ProductId = route.ProductId,
				BrandName = product?.Brand?.Name ?? "Không rõ",
				SubCategoryName = product?.Category?.Name ?? "Không rõ",
				PickUpItemImages = productImages,

				// Address & Geo
				Address = address,
				Iat = userAddress?.Iat ?? 0,
				Ing = userAddress?.Ing ?? 0,

				// People
				Sender = senderModel,
				Collector = collectorModel,

				// Route Detail
				CollectionDate = route.CollectionDate,
				EstimatedTime = route.EstimatedTime,
				Actual_Time = route.Actual_Time,
				Status = route.Status,
				DistanceKm = route.DistanceKm,
				LicensePlate = vehicle?.Plate_Number ?? "Chưa gán xe",

				// Confirm Images (Xử lý null)
				ConfirmImages = route.ConfirmImages ?? new List<string>()
			};
		}
		public async Task<PagedResultModel<CollectionRouteModel>> GetPagedRoutes(RouteSearchQueryModel parameters)
		{
			// 1. Chuẩn bị tham số phân trang
			int page = parameters.Page <= 0 ? 1 : parameters.Page;
			int limit = parameters.Limit <= 0 ? 10 : parameters.Limit;

			// 2. Chuyển đổi PickUpDate từ DateTime? sang DateOnly? (nếu Model truyền vào là DateTime)
			DateOnly? dateParam = null;
			if (parameters.PickUpDate.HasValue)
			{
				dateParam =parameters.PickUpDate.Value;
			}

			// 3. Gọi Repository
			// (Lưu ý: Repo nhận collectionPointId là string, DateOnly?, status, page, limit)
			var (routes, totalItems) = await _collectionRouteRepository.GetPagedRoutesAsync(
				collectionPointId: parameters.CollectionPointId, // Giả sử model truyền string
				pickUpDate: dateParam,
				status: parameters.Status,
				page: page,
				limit: limit
			);

			// 4. Map dữ liệu sang Model
			var results = new List<CollectionRouteModel>();

			foreach (var r in routes)
			{
				// Await từng cái một -> Tránh lỗi "A second operation..."
				var model = await BuildCollectionRouteModel(r);

				if (model != null)
				{
					results.Add(model);
				}
			}


			// 5. Trả về kết quả
			return new PagedResultModel<CollectionRouteModel>(
				results,
				page,
				limit,
				totalItems
			);
		}

		
	}
}
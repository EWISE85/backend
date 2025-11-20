using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Services
{
	public class ProductService : IProductService
	{
		private readonly List<Products> _products = FakeDataSeeder.products;
		private readonly List<SizeTier> _sizeTiers = FakeDataSeeder.sizeTiers;
		private readonly List<ProductValues> _productValues = FakeDataSeeder.productValues;
		private readonly List<Attributes> _attributes = FakeDataSeeder.attributes;
		private readonly List<Brand> _brands = FakeDataSeeder.brands;
		private readonly List<CollectionRoutes> _collectionRoutes = FakeDataSeeder.collectionRoutes;
		private readonly List<CollectionGroups> _collectionGroups = FakeDataSeeder.collectionGroups;
		private readonly List<Post> _posts = FakeDataSeeder.posts;
		private readonly List<Shifts> _shifts = FakeDataSeeder.shifts;
		private readonly List<Vehicles> _vehicles = FakeDataSeeder.vehicles;
		private readonly List<Category> _categories = FakeDataSeeder.categories;
		private readonly List<PostImages> _postImages = FakeDataSeeder.postImages;
		private readonly List<ProductImages> productImages = FakeDataSeeder.productImages;
		private readonly IPointTransactionService _pointTransactionService;
		private readonly List<ProductStatusHistory> _productStatusHistories = FakeDataSeeder.productStatusHistories;
		private readonly IUserService _userService;
		private readonly ICollectorService _collectorService;
		public ProductService(IPointTransactionService pointTransactionService, IUserService userService, ICollectorService collectorService)
		{
			_pointTransactionService = pointTransactionService;
			_userService = userService;
			_collectorService = collectorService;
		}
		public bool AddPackageIdToProductByQrCode(string qrCode, string? packageId)
		{
			var product = _products.FirstOrDefault(p => p.QRCode == qrCode);
			if (product == null)
			{
				return false;
			}

			product.PackageId = packageId;
			return true;
		}

		public ProductDetailModel AddProduct(CreateProductAtWarehouseModel createProductRequest)
		{
			var newProduct = new Products
			{
				Id = Guid.NewGuid(),
				CategoryId = createProductRequest.SubCategoryId,
				BrandId = createProductRequest.BrandId,
				Description = createProductRequest.Description,
				QRCode = createProductRequest.QrCode,
				CreateAt = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7)),
				SmallCollectionPointId = createProductRequest.SmallCollectionPointId,
				Status = "Nhập kho"
			};
			_products.Add(newProduct);
			for (int i = 0; i < createProductRequest.Images.Count; i++)
			{
				var newPostImage = new ProductImages
				{
					ImageUrl = createProductRequest.Images[i],
					ProductId = newProduct.Id,
					ProductImagesId = Guid.NewGuid()
				};
				productImages.Add(newPostImage);
			}
			var pointTransaction = new CreatePointTransactionModel
			{
				UserId = createProductRequest.SenderId,
				Point = createProductRequest.Point,
				Desciption = "Điểm nhận được khi gửi sản phẩm tại kho",
			};
			_pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction);
			return new ProductDetailModel
			{
				ProductId = newProduct.Id,
				Description = newProduct.Description,
				CategoryId = newProduct.CategoryId,
				BrandId = newProduct.BrandId,
				BrandName = _brands.FirstOrDefault(b => b.BrandId == newProduct.BrandId)?.Name,
				CategoryName = _categories.FirstOrDefault(c => c.Id == newProduct.CategoryId)?.Name,
				QrCode = newProduct.QRCode,
				Status = newProduct.Status
			};
		}

		public Products? GetById(Guid productId)
		{
			return _products.FirstOrDefault(p => p.Id == productId);
		}

		public ProductComeWarehouseDetailModel? GetByQrCode(string qrcode)
		{
			// 1. Tìm Product theo QR Code
			var product = _products.FirstOrDefault(p => p.QRCode == qrcode);
			if (product == null)
			{
				return null;
			}

			// 2. Tìm Post (Bài đăng) liên quan đến Product này
			// (Cần bước này để lấy được danh sách ảnh từ bảng _postImages)
			var post = _posts.FirstOrDefault(p => p.ProductId == product.Id);

			// 3. Lấy các thông tin tham chiếu (Brand, Category, SizeTier)
			var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);
			var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
			var sizeTier = _sizeTiers.FirstOrDefault(st => st.SizeTierId == product.SizeTierId);

			// 4. Lấy danh sách ảnh (Nếu tìm thấy Post)
			var imageUrls = new List<string>();

			double? point = null;
			if (post != null)
			{
				imageUrls = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();
				point = post.EstimatePoint;
			}

			// 5. Lấy danh sách thuộc tính (Attributes) - Giống hệt logic hàm dưới
			var attributesList = _productValues
				.Where(pv => pv.ProductId == product.Id)
				.Select(pv =>
				{
					var attribute = _attributes.FirstOrDefault(a => a.Id == pv.AttributeId);
					return new ProductValueDetailModel
					{
						AttributeName = attribute?.Name ?? "N/A",
						Value = pv.Value.ToString(),
					};
				})
				.ToList();

			// 6. Trả về model đầy đủ (Mapping chuẩn theo hàm ProductsComeWarehouseByDate)
			return new ProductComeWarehouseDetailModel
			{
				ProductId = product.Id,
				Description = product.Description,
				BrandId = brand?.BrandId ?? Guid.Empty,
				BrandName = brand?.Name ?? "N/A",
				CategoryId = category?.Id ?? Guid.Empty,
				CategoryName = category?.Name ?? "N/A",
				ProductImages = imageUrls, // Đã bổ sung ảnh
				QrCode = product.QRCode,
				Status = product.Status,
				SizeTierName = sizeTier?.Name, // Có thể null
				EstimatePoint = point, // Có thể null
				Attributes = attributesList
			};
		}

		public List<ProductDetailModel> GetProductsByPackageId(string packageId)
		{
			// 1. Lọc ra các sản phẩm thuộc package
			var productsInPackage = _products
				.Where(p => p.PackageId == packageId);

			// 2. Dùng .Select để biến đổi TỪNG sản phẩm
			var productDetails = productsInPackage.Select(p =>
			{
				// 3. "Join" bằng tay với SizeTiers
				var sizeTier = _sizeTiers
					.FirstOrDefault(st => st.SizeTierId == p.SizeTierId);
				var brand = _brands
					.FirstOrDefault(b => b.BrandId == p.BrandId);
				var category = _categories
					.FirstOrDefault(c => c.Id == p.CategoryId);
				// 4. "Join" bằng tay với ProductValues và Attributes
				var attributesList = _productValues
					.Where(pv => pv.ProductId == p.Id) // Lấy các value của sản phẩm này
					.Select(pv =>
					{
						// Với mỗi value, tìm attribute tương ứng
						var attribute = _attributes
							.FirstOrDefault(a => a.Id == pv.AttributeId);

						return new ProductValueDetailModel
						{
							AttributeName = attribute?.Name ?? "N/A",
							Value = pv.Value.ToString(),
						};
					})
					.ToList();

				// 5. Xây dựng model hoàn chỉnh
				return new ProductDetailModel
				{
					ProductId = p.Id,
					Description = p.Description,
					BrandName = brand?.Name,
					BrandId = brand.BrandId,
					CategoryId = category.Id,
					CategoryName = category.Name,
					QrCode = p.QRCode,
					SizeTierName = sizeTier?.Name,
					Attributes = attributesList,
					Status = p.Status
				};
			})
			.ToList();

			return productDetails;
		}

		public List<ProductComeWarehouseDetailModel> ProductsComeWarehouseByDate(DateOnly fromDate, DateOnly toDate, int smallCollectionPointId)
		{
			// =================================================================================
			// PHẦN 1: LẤY SẢN PHẨM TỪ TUYẾN THU GOM (Có Route)
			// Điều kiện: Route thuộc trạm này & Ngày thu gom nằm trong khoảng [fromDate, toDate]
			// =================================================================================
			var routeModels = new List<ProductComeWarehouseDetailModel>();

			// 1. Lấy danh sách xe của trạm
			var vehicleIds = _vehicles
				.Where(v => v.Small_Collection_Point == smallCollectionPointId)
				.Select(v => v.Id)
				.ToList();

			if (vehicleIds.Any())
			{
				// 2. Lấy Shift liên quan đến các xe này (để tìm Group -> Route)
				// Lưu ý: Ở đây ta chưa lọc ngày của Shift vội, vì logic chính nằm ở ngày của Route
				var shiftIds = _shifts
					.Where(s => vehicleIds.Contains(s.Vehicle_Id))
					.Select(s => s.Id)
					.ToList();

				if (shiftIds.Any())
				{
					var groupIds = _collectionGroups
						.Where(g => shiftIds.Contains(g.Shift_Id))
						.Select(g => g.Id)
						.ToList();

					// 3. Lọc Route theo khoảng thời gian
					var routesInRange = _collectionRoutes
						.Where(r =>
							groupIds.Contains(r.CollectionGroupId) &&
							r.CollectionDate >= fromDate && // Từ ngày
							r.CollectionDate <= toDate      // Đến ngày
						)
						.ToList();

					// 4. Map dữ liệu từ Route -> Post -> Product
					routeModels = routesInRange.Select(route =>
					{
						var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
						if (post == null) return null;
						var product = _products.FirstOrDefault(p => p.Id == post.ProductId);
						if (product == null) return null;

						return MapToDetailModel(product, post);
					})
					.Where(x => x != null)
					.ToList()!;
				}
			}

			// =================================================================================
			// PHẦN 2: LẤY SẢN PHẨM TẠO TRỰC TIẾP TẠI KHO
			// Điều kiện: Thuộc trạm này & Ngày tạo nằm trong khoảng [fromDate, toDate]
			// =================================================================================

			var directProducts = _products
				.Where(p =>
					p.SmallCollectionPointId == smallCollectionPointId && // Đúng trạm
					p.CreateAt != null &&                                 // Ngày tạo không null
					p.CreateAt >= fromDate &&                             // Từ ngày
					p.CreateAt <= toDate &&                               // Đến ngày
					p.PackageId == null &&                                // Chưa đóng gói
					p.Status == "Nhập kho"                                // Trạng thái mặc định
				)
				.ToList();

			var directModels = directProducts.Select(product =>
			{
				// Truyền null vào post vì không có bài đăng
				return MapToDetailModel(product, null);
			}).ToList();

			// =================================================================================
			// PHẦN 3: GỘP DỮ LIỆU VÀ TRẢ VỀ
			// =================================================================================

			var combinedList = routeModels
				.Concat(directModels)
				.DistinctBy(x => x.ProductId) 
				.OrderByDescending(x => x.Status) 
				.ToList();

			return combinedList;
		}



		// Hàm Map giữ nguyên như cũ
		private ProductComeWarehouseDetailModel MapToDetailModel(Products product, Post? post)
		{
			var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);
			var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
			var sizeTier = _sizeTiers.FirstOrDefault(st => st.SizeTierId == product.SizeTierId);

			// Lấy ảnh (chỉ có nếu post tồn tại)
			var imageUrls = new List<string>();
			if (post != null)
			{
				imageUrls = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();
			}
			else {
				imageUrls = productImages
					.Where(img => img.ProductId == product.Id)
					.Select(img => img.ImageUrl)
					.ToList();
			}


			// Lấy thuộc tính
			var attributesList = _productValues
				.Where(pv => pv.ProductId == product.Id)
				.Select(pv =>
				{
					var attribute = _attributes.FirstOrDefault(a => a.Id == pv.AttributeId);
					return new ProductValueDetailModel
					{
						AttributeName = attribute?.Name ?? "N/A",
						Value = pv.Value.ToString(),
					};
				})
				.ToList();

			return new ProductComeWarehouseDetailModel
			{
				ProductId = product.Id,
				Description = product.Description,
				BrandId = brand?.BrandId ?? Guid.Empty,
				BrandName = brand?.Name ?? "N/A",
				CategoryId = category?.Id ?? Guid.Empty,
				CategoryName = category?.Name ?? "N/A",
				ProductImages = imageUrls,
				QrCode = product.QRCode,
				Status = product.Status,
				SizeTierName = sizeTier?.Name,
				EstimatePoint = post.EstimatePoint,
				Attributes = attributesList
			};
		}

		public bool UpdateProductStatusByQrCode(string productQrCode, string status)
		{
			var product = _products.FirstOrDefault(p => p.QRCode == productQrCode);
			if (product == null)
			{
				return false;
			}

			product.Status = status;
			return true;
		}

		public bool UpdateProductStatusByQrCodeAndPlusUserPoint(string productQrCode, string status, UserReceivePointFromCollectionPointModel model)
		{
			var product = _products.FirstOrDefault(p => p.QRCode == productQrCode);
			if (product == null)
			{
				return false;
			}

			var post = _posts.FirstOrDefault(p => p.ProductId == product.Id);
			if (post == null)
			{
				return false;
			}
			var pointTransaction = new CreatePointTransactionModel
			{
				PostId = post.Id,
				UserId = post.SenderId,
				Point = model.Point,
				Desciption = model.Description,
			};
			product.Status = status;
			var newHistory = new ProductStatusHistory
			{
				ProductStatusHistoryId = Guid.NewGuid(),
				ProductId = product.Id,
				ChangedAt = DateTime.UtcNow,
				StatusDescription = "Sản phẩm đã về đến kho",
				Status = status
			};	
			_pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction);
			return true;
		}

		public List<ProductComeWarehouseDetailModel> GetAllProductsByUserId(Guid userId)
		{
			var userPosts = _posts.Where(p => p.SenderId == userId).ToList();

			var productDetails = userPosts.Select(post =>
			{
				var product = _products.FirstOrDefault(p => p.Id == post.ProductId);
				if (product == null) return null;

				return MapToDetailModel(product, post);
			})
			.Where(x => x != null)
			.ToList()!;

			return productDetails;
		}

		public ProductDetail? GetProductDetailById(Guid productId)
		{
			// 1. Tìm Product
			var product = _products.FirstOrDefault(p => p.Id == productId);
			if (product == null) return null;

			// 2. Tìm Post
			var post = _posts.FirstOrDefault(p => p.ProductId == productId);
			if (post == null) return null;

			// 3. Lấy Category và Brand
			var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
			var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);

			// 4. Lấy Sender
			var sender = _userService.GetById(post.SenderId);

			// 5. Xử lý SizeTier / Attributes
			string? sizeTierName = null;
			List<ProductValueDetailModel>? productAttributes = null;

			if (product.SizeTierId.HasValue)
			{
				sizeTierName = _sizeTiers
					.FirstOrDefault(st => st.SizeTierId == product.SizeTierId.Value)?.Name;
			}
			else
			{
				productAttributes = _productValues
					.Where(pv => pv.ProductId == product.Id)
					.Join(_attributes,
						  pv => pv.AttributeId,
						  attr => attr.Id,
						  (pv, attr) => new ProductValueDetailModel
						  {
							  AttributeName = attr.Name,
							  Value = pv.Value.ToString()
						  })
					.ToList();
			}

			// 6. Xử lý Schedule
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			List<DailyTimeSlots> schedule = new List<DailyTimeSlots>();
			if (!string.IsNullOrEmpty(post.ScheduleJson))
			{
				try { schedule = JsonSerializer.Deserialize<List<DailyTimeSlots>>(post.ScheduleJson, options) ?? new List<DailyTimeSlots>(); }
				catch (JsonException) { schedule = new List<DailyTimeSlots>(); }
			}

			// 7. Lấy ảnh
			var imageUrls = _postImages.Where(pi => pi.PostId == post.Id).Select(pi => pi.ImageUrl).ToList();

			// =================================================================================
			// 8. TÌM THÔNG TIN LỊCH TRÌNH VÀ COLLECTOR (LOGIC MỚI)
			// =================================================================================

			Collector? collector = null;
			DateOnly? pickUpDate = null;
			TimeOnly? estimatedTime = null;

			// Bước A: Tìm Route dựa trên PostId
			var route = _collectionRoutes.FirstOrDefault(r => r.PostId == post.Id);

			if (route != null)
			{
				// 1. Lấy thông tin ngày giờ từ Route
				pickUpDate = route.CollectionDate;
				estimatedTime = route.EstimatedTime;

				// Bước B: Tìm Group để lấy Shift (Route -> Group)
				var group = _collectionGroups.FirstOrDefault(g => g.Id == route.CollectionGroupId);

				if (group != null)
				{
					// Bước C: Tìm Shift để lấy CollectorId (Group -> Shift)
					var shift = _shifts.FirstOrDefault(s => s.Id == group.Shift_Id);

					if (shift != null)
					{
						
						collector = _collectorService.GetById(shift.CollectorId); 
					}
				}
			}

			// 9. Return kết quả
			return new ProductDetail
			{
				ProductId = product.Id,
				CategoryId = product.CategoryId,
				CategoryName = category?.Name ?? "Không rõ",
				BrandId = product.BrandId,
				BrandName = brand?.Name ?? "Không rõ",
				Description = product.Description,
				ProductImages = imageUrls,
				Status = post.Status,
				SizeTierName = sizeTierName,
				EstimatePoint = post.EstimatePoint,
				Sender = sender,
				Address = post.Address,
				Schedule = schedule,
				Attributes = productAttributes,

				// === Dữ liệu từ Route/Shift ===
				Collector = collector,
				PickUpDate = pickUpDate,
				EstimatedTime = estimatedTime
			};
		}
	}
}

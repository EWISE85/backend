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
		public ProductService(IPointTransactionService pointTransactionService)
		{
			_pointTransactionService = pointTransactionService;
		}
		public bool AddPackageIdToProductByQrCode(string qrCode, string packageId)
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
					Attributes = attributesList
				};
			})
			.ToList();

			return productDetails;
		}

		public PagedResult<ProductComeWarehouseDetailModel> ProductsComeWarehouseByDate(int page, int limit, DateOnly pickUpDate, int smallCollectionPointId, string status)
		{
			// Helper trả về rỗng
			PagedResult<ProductComeWarehouseDetailModel> ReturnEmpty()
			{
				return new PagedResult<ProductComeWarehouseDetailModel>
				{
					Page = page < 1 ? 1 : page,
					Limit = limit < 1 ? 10 : limit,
					TotalItems = 0,
					Data = new List<ProductComeWarehouseDetailModel>()
				};
			}

			if (page < 1) page = 1;
			if (limit < 1) limit = 10;

			
			var routeModels = new List<ProductComeWarehouseDetailModel>();

			var vehicleIds = _vehicles
				.Where(v => v.Small_Collection_Point == smallCollectionPointId)
				.Select(v => v.Id)
				.ToList();

			if (vehicleIds.Any())
			{
				// Tìm các ca làm việc trong ngày
				var shiftIds = _shifts
					.Where(s => s.WorkDate == pickUpDate && vehicleIds.Contains(s.Vehicle_Id))
					.Select(s => s.Id).ToList();

				if (shiftIds.Any())
				{
					var groupIds = _collectionGroups.Where(g => shiftIds.Contains(g.Shift_Id)).Select(g => g.Id).ToList();

					var routesOfTheDay = _collectionRoutes
						.Where(r => r.CollectionDate == pickUpDate && groupIds.Contains(r.CollectionGroupId))
						.ToList();

					// Map dữ liệu
					routeModels = routesOfTheDay.Select(route =>
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

			

			var directProducts = _products
				.Where(p =>
					p.SmallCollectionPointId == smallCollectionPointId && 
					p.CreateAt == pickUpDate &&                          
					p.PackageId == null &&                               
					p.Status == "Nhập kho"                              
				)
				.ToList();

			// Map dữ liệu (Post = null vì không có bài đăng)
			var directModels = directProducts.Select(product =>
			{
				return MapToDetailModel(product, null);
			}).ToList();


			
			var combinedList = routeModels
				.Concat(directModels)
				.DistinctBy(x => x.ProductId) 
				.ToList();

			if (!string.IsNullOrEmpty(status))
			{
				combinedList = combinedList
					.Where(model => model.Status.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase))
					.ToList();
			}

			// 3. Phân trang (In-Memory Pagination)
			var totalItems = combinedList.Count;
			var pagedData = combinedList
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			return new PagedResult<ProductComeWarehouseDetailModel>
			{
				Page = page,
				Limit = limit,
				TotalItems = totalItems,
				Data = pagedData
			};
		}

		// Hàm Map giữ nguyên như cũ
		private ProductComeWarehouseDetailModel MapToDetailModel(Products product, Post? post)
		{
			// ... (Code mapping y hệt câu trả lời trước)
			var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);
			var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
			var sizeTier = _sizeTiers.FirstOrDefault(st => st.SizeTierId == product.SizeTierId);

			var imageUrls = new List<string>();

			if (post != null)
			{
				imageUrls = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();
			}
			else
			{
				imageUrls = productImages
					.Where(img => img.ProductId == product.Id)
					.Select(img => img.ImageUrl)
					.ToList();
			}

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
			_pointTransactionService.ReceivePointFromCollectionPoint(pointTransaction);
			return true;
		}
	}
}

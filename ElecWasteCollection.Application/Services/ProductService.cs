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

		public Products? GetById(Guid productId)
		{
			return _products.FirstOrDefault(p => p.Id == productId);
		}

		public Products? GetByQrCode(string qrcode)
		{
			return _products.FirstOrDefault(p => p.QRCode == qrcode);
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
					SizeTierName = sizeTier?.Name,
					Attributes = attributesList
				};
			})
			.ToList();

			return productDetails;
		}

		public PagedResult<ProductComeWarehouseDetailModel> ProductsComeWarehouseByDate(int page, int limit, DateOnly pickUpDate, int smallCollectionPointId, string status)
		{
			// Helper để trả về kết quả rỗng nhanh gọn
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

			// Chuẩn hóa input phân trang
			if (page < 1) page = 1;
			if (limit < 1) limit = 10;

			// 1. Tìm các Xe (Vehicles) thuộc về trạm thu gom này
			var vehicleIds = _vehicles
				.Where(v => v.Small_Collection_Point == smallCollectionPointId)
				.Select(v => v.Id)
				.ToList();

			if (!vehicleIds.Any()) return ReturnEmpty();

			// 2. Tìm các Ca làm việc (Shifts) trong ngày đó sử dụng các xe trên
			var shiftIds = _shifts
				.Where(s => s.WorkDate == pickUpDate && vehicleIds.Contains(s.Vehicle_Id))
				.Select(s => s.Id)
				.ToList();

			if (!shiftIds.Any()) return ReturnEmpty();

			// 3. Tìm các Nhóm (Groups) thuộc các ca làm việc đó
			var groupIds = _collectionGroups
				.Where(g => shiftIds.Contains(g.Shift_Id))
				.Select(g => g.Id)
				.ToList();

			// 4. Lọc các Tuyến (Routes) thuộc các nhóm đó VÀ đúng ngày
			var routesOfTheDay = _collectionRoutes
				.Where(r => r.CollectionDate == pickUpDate && groupIds.Contains(r.CollectionGroupId))
				.ToList();

			if (!routesOfTheDay.Any()) return ReturnEmpty();

			// 5. Mapping dữ liệu (Tạo model đầy đủ)
			// Lưu ý: Logic này chạy trong RAM (IEnumerable) vì routesOfTheDay đã ToList() ở trên
			var query = routesOfTheDay.Select(route =>
			{
				var post = _posts.FirstOrDefault(p => p.Id == route.PostId);
				if (post == null) return null;

				var product = _products.FirstOrDefault(p => p.Id == post.ProductId);
				if (product == null) return null;

				var brand = _brands.FirstOrDefault(b => b.BrandId == product.BrandId);
				var category = _categories.FirstOrDefault(c => c.Id == product.CategoryId);
				var sizeTier = _sizeTiers.FirstOrDefault(st => st.SizeTierId == product.SizeTierId);

				var imageUrls = _postImages
					.Where(img => img.PostId == post.Id)
					.Select(img => img.ImageUrl)
					.ToList();

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
					Status = product.Status,
					SizeTierName = sizeTier?.Name ?? null,
					Attributes = attributesList
				};
			})
			.Where(model => model != null); // Lọc bỏ null

			// 6. === LỌC STATUS ===
			if (!string.IsNullOrEmpty(status))
			{
				query = query.Where(model => model.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
			}

			// 7. === PHÂN TRANG ===
			// Lưu ý: Vì query là IEnumerable (In-Memory), ta cần ép ToList() để lấy số lượng chính xác trước khi Skip
			var filteredList = query.ToList();

			var totalItems = filteredList.Count;

			var pagedData = filteredList
				.Skip((page - 1) * limit)
				.Take(limit)
				.ToList();

			// 8. Trả về kết quả PagedResult
			return new PagedResult<ProductComeWarehouseDetailModel>
			{
				Page = page,
				Limit = limit,
				TotalItems = totalItems,
				Data = pagedData
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
	}
}

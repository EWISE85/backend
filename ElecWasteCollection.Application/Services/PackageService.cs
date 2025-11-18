using ElecWasteCollection.Application.Data;
using ElecWasteCollection.Application.IServices;
using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenCvSharp.Stitcher;

namespace ElecWasteCollection.Application.Services
{
	public class PackageService : IPackageService
	{
		private readonly List<Packages> packages = FakeDataSeeder.packages;
		private readonly IProductService _productService;
		private readonly List<ProductStatusHistory> _productStatusHistories = FakeDataSeeder.productStatusHistories;


		public PackageService( IProductService productService)
		{
			_productService = productService;
		}

		public string CreatePackageAsync(CreatePackageModel model)
		{
			var newPackage = new Packages
			{
				PackageId = model.PackageId,
				PackageName = model.PackageName,
				SmallCollectionPointsId = model.SmallCollectionPointsId,
				CreateAt = DateTime.UtcNow,
				Status = "Đang đóng gói"
			};
			packages.Add(newPackage);
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = _productService.GetByQrCode(qrCode);

				if (product != null)
				{
					_productService.AddPackageIdToProductByQrCode(product.QrCode, newPackage.PackageId);
					_productService.UpdateProductStatusByQrCode(product.QrCode, "Đã đóng thùng");
					var newHistory = new ProductStatusHistory
					{
						ProductStatusHistoryId = Guid.NewGuid(),
						ProductId = product.ProductId,
						ChangedAt = DateTime.UtcNow,
						StatusDescription = "Sản phẩm đã được đóng gói",
						Status = "Đã đóng thùng"
					};

				}
			}

			return newPackage.PackageId;
		}

		public PackageDetailModel GetPackageById(string packageId)
		{
			var package = packages.FirstOrDefault(p => p.PackageId == packageId);
			if (package == null)
			{
				return null;
			}

			var productDetails = _productService.GetProductsByPackageId(packageId);

			var packageDetail = new PackageDetailModel
			{
				PackageId = package.PackageId,
				PackageName = package.PackageName,
				SmallCollectionPointsId = package.SmallCollectionPointsId,
				Status = package.Status,
				Products = productDetails
			};

			return packageDetail;
		}

		public PagedResult<PackageDetailModel> GetPackagesByQuery(PackageSearchQueryModel query)
		{
			var filteredData = packages.AsEnumerable();

			
			if (query.SmallCollectionPointsId > 0)
			{
				filteredData = filteredData.Where(p => p.SmallCollectionPointsId == query.SmallCollectionPointsId);
			}

			if (!string.IsNullOrEmpty(query.Status))
			{
				filteredData = filteredData.Where(p =>
					!string.IsNullOrEmpty(p.Status) &&
					p.Status.Equals(query.Status.Trim(), StringComparison.OrdinalIgnoreCase));
			}

			int totalCount = filteredData.Count();

			var pagedPackages = filteredData
				.Skip((query.Page - 1) * query.Limit)
				.Take(query.Limit)
				.ToList();

			var resultItems = new List<PackageDetailModel>();

			foreach (var pkg in pagedPackages)
			{
				var productDetails = _productService.GetProductsByPackageId(pkg.PackageId);

				var model = new PackageDetailModel
				{
					PackageId = pkg.PackageId,
					PackageName = pkg.PackageName, 
					SmallCollectionPointsId = pkg.SmallCollectionPointsId,
					Products = productDetails
				};

				resultItems.Add(model);
			}

			// 6. Trả về kết quả
			return new PagedResult<PackageDetailModel>
			{
				Data = resultItems,
				TotalItems = totalCount,
				Page = query.Page,
				Limit = query.Limit,
			};
		}

		public bool UpdatePackageAsync(UpdatePackageModel model)
		{
			// 1. Tìm gói hàng cần update
			var package = packages.FirstOrDefault(p => p.PackageId == model.PackageId);
			if (package == null)
			{
				return false;
			}

			// 2. Update thông tin cơ bản
			package.PackageName = model.PackageName;
			package.SmallCollectionPointsId = model.SmallCollectionPointsId;

			// 3. Lấy danh sách sản phẩm HIỆN TẠI đang thuộc về gói này (trong Database/List cũ)
			var currentProductsInPackage = _productService.GetProductsByPackageId(model.PackageId);

			// Tạo HashSet từ danh sách QR Code MỚI gửi lên để tra cứu cho nhanh
			var newQrCodesSet = model.ProductsQrCode.ToHashSet();

			// === XỬ LÝ XÓA (REMOVE) ===
			// Duyệt qua các sản phẩm cũ, nếu cái nào KHÔNG nằm trong danh sách mới -> Đuổi khỏi gói
			foreach (var existingProduct in currentProductsInPackage)
			{
				if (!newQrCodesSet.Contains(existingProduct.QrCode))
				{
					// Set lại PackageId = null
					// Lưu ý: Bạn cần đảm bảo hàm này nhận tham số null, hoặc viết hàm riêng để Remove
					_productService.AddPackageIdToProductByQrCode(existingProduct.QrCode, null);

					// Trả lại trạng thái ban đầu (ví dụ: "Nhập kho")
					_productService.UpdateProductStatusByQrCode(existingProduct.QrCode, "Nhập kho");

					var oldHistory = _productStatusHistories.FirstOrDefault(h => h.ProductId == existingProduct.ProductId && h.Status == "Đã đóng thùng");
					if (oldHistory != null)
					{
						_productStatusHistories.Remove(oldHistory);
					}

					}
			}

			// === XỬ LÝ THÊM (ADD) HOẶC UPDATE ===
			// Duyệt danh sách mới gửi lên để gán vào gói
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = _productService.GetByQrCode(qrCode);
				if (product != null)
				{
					// Gán vào gói hiện tại
					_productService.AddPackageIdToProductByQrCode(product.QrCode, package.PackageId);

					// Cập nhật trạng thái
					_productService.UpdateProductStatusByQrCode(product.QrCode, "Đã đóng thùng");
				}
			}
			return true;
		}

		public bool UpdatePackageStatus(string packageId, string status)
		{
			var package = packages.FirstOrDefault(p => p.PackageId == packageId);
			if (package == null)
			{
				return false;
			}

			package.Status = status;
			return true;
		}
	}
}

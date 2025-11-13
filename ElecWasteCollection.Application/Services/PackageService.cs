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
	public class PackageService : IPackageService
	{
		private readonly List<Packages> packages = FakeDataSeeder.packages;
		private readonly IProductService _productService;


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
				Status = "Đã đóng thùng"
			};
			packages.Add(newPackage);
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = _productService.GetByQrCode(qrCode);

				if (product != null)
				{
					_productService.AddPackageIdToProductByQrCode(product.QrCode, newPackage.PackageId);
					_productService.UpdateProductStatusByQrCode(product.QrCode, "Đã đóng thùng");
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
	}
}

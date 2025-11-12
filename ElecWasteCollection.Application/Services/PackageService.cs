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
				Status = "Sealed"
			};
			packages.Add(newPackage);
			foreach (var qrCode in model.ProductsQrCode)
			{
				var product = _productService.GetByQrCode(qrCode);

				if (product != null)
				{
					_productService.AddPackageIdToProductByQrCode(product.QRCode, newPackage.PackageId);
					_productService.UpdateProductStatusByQrCode(product.QRCode, "In Package");
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
	}
}

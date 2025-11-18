using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IProductService
	{
		Products? GetById(Guid productId);

		ProductComeWarehouseDetailModel? GetByQrCode(string qrcode);

		ProductDetailModel AddProduct(CreateProductAtWarehouseModel createProductRequest);

		bool AddPackageIdToProductByQrCode(string productQrCode, string? packageId);

		List<ProductDetailModel> GetProductsByPackageId(string packageId);

		bool UpdateProductStatusByQrCode(string productQrCode, string status);
		bool UpdateProductStatusByQrCodeAndPlusUserPoint(string productQrCode, string status, UserReceivePointFromCollectionPointModel model );

		List<ProductComeWarehouseDetailModel> ProductsComeWarehouseByDate(DateOnly fromDate, DateOnly toDate, int smallCollectionPointId);

		List<ProductComeWarehouseDetailModel> GetAllProductsByUserId(Guid userId);

	}
}

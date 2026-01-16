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
		Task<ProductDetailModel> GetById(Guid productId);

		Task<ProductComeWarehouseDetailModel> GetByQrCode(string qrcode);

		Task<ProductDetailModel> AddProduct(CreateProductAtWarehouseModel createProductRequest);

		Task<bool> AddPackageIdToProductByQrCode(string qrCode, string? packageId);

		Task<List<ProductDetailModel>> GetProductsByPackageIdAsync(string packageId);

		Task<bool> UpdateProductStatusByQrCode(string productQrCode, string status);
		Task<bool> UpdateProductStatusByProductId(Guid productId, string status);

		Task<bool> UpdateProductStatusByQrCodeAndPlusUserPoint(string productQrCode, string status, UserReceivePointFromCollectionPointModel model);

		Task<List<ProductComeWarehouseDetailModel>> ProductsComeWarehouseByDateAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId);


		Task<List<ProductComeWarehouseDetailModel>> GetAllProductsByUserId(Guid userId);

		Task<ProductDetail?> GetProductDetailByIdAsync(Guid productId);

		Task<bool> UpdateCheckedProductAtRecycler(string packageId, List<string> QrCode);

		Task<PagedResultModel<ProductDetail>> AdminGetProductsAsync(AdminFilterProductModel model);

		Task<bool> CancelProduct(Guid productId, string rejectMessage);

	}
}

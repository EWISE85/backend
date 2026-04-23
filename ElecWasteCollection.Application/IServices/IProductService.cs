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

		Task<bool> RemovePackageIdFromProductByQrCode(string qrCode);

		Task<List<ProductDetailModel>> GetProductsByPackageIdAsync(string packageId);

		Task<bool> UpdateProductStatusByQrCode(string productQrCode, string status);
		Task<bool> UpdateProductStatusByProductId(Guid productId, string status);

		Task<bool> ReceiveProductAtWarehouse(List<UserReceivePointFromCollectionPointModel> models);

		Task<List<ProductComeWarehouseDetailModel>> ProductsComeWarehouseByDateAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId);


		Task<PagedResultModel<ProductComeWarehouseDetailModel>> GetAllProductsByUserId(string? search, DateOnly? createAt, Guid userId, int page, int limit);

		Task<ProductDetail?> GetProductDetailByIdAsync(Guid productId);

		Task<bool> UpdateCheckedProductAtRecycler(string packageId, List<string> QrCode);

		Task<PagedResultModel<ProductDetail>> AdminGetProductsAsync(AdminFilterProductModel model);

		Task<bool> CancelProduct(Guid productId, string rejectMessage);
		Task<PagedResultModel<ProductDetailModel>> GetProductsByPackageIdAsync(string packageId, int page, int limit);

		Task<List<ProductComeWarehouseDetailModel>> GetProductNeedToPickUp(Guid userId, DateOnly pickUpDate);

		Task<bool> SeederQrCodeInProduct(List<Guid> productIds, List<string> QrCode);
		Task<bool> RevertProductStatusByQrCodeAndMinusUserPoint(string productQrCode);
		Task<bool> CheckExistingQRCode(string qrCode);
	}
}

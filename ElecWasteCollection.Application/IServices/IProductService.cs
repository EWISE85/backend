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

		Products? GetByQrCode(string qrcode);

		bool AddPackageIdToProductByQrCode(string productQrCode, string packageId);

		List<ProductDetailModel> GetProductsByPackageId(string packageId);

		bool UpdateProductStatusByQrCode(string productQrCode, string status);

		PagedResult<ProductComeWarehouseDetailModel> ProductsComeWarehouseByDate(int page, int limit,DateOnly pickUpDate, int smallCollectionPointId, string status);
	}
}

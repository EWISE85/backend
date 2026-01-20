using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IProductRepository : IGenericRepository<Products>
	{
		Task<Products?> GetProductByQrCodeWithDetailsAsync(string qrcode);
		Task<List<Products>> GetProductsByPackageIdWithDetailsAsync(string packageId);
		Task<List<Products>> GetProductsCollectedByRouteAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId);

		Task<List<Products>> GetDirectlyEnteredProductsAsync(DateOnly fromDate, DateOnly toDate, string smallCollectionPointId);
		Task<Products?> GetProductWithDetailsAsync(Guid productId);
		Task<List<Products>> GetProductsBySenderIdWithDetailsAsync(Guid senderId);
		Task<Products?> GetProductDetailWithAllRelationsAsync(Guid productId);
			Task<(List<Products> Items, int TotalCount)> GetPagedProductsForAdminAsync(
				int page,
				int limit,
				DateOnly? fromDate,
				DateOnly? toDate,
				string? categoryName,
				string? collectionCompanyId
			);
		Task<Dictionary<string, int>> GetProductCountsByCategoryAsync(DateTime from, DateTime to);
		Task<(List<Products> Items, int TotalCount)> GetPagedProductsByPackageIdAsync(string packageId, int page, int limit);
	}
}

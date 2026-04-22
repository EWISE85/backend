using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICollectionUnitService
    {
		Task<bool> AddNewSmallCollectionPoint(CollectionUnit smallCollectionPoints);
		Task<bool> UpdateSmallCollectionPoint(CollectionUnit smallCollectionPoints);

		Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId);

		Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointByCompanyId(string companyId);

		Task<SmallCollectionPointsResponse> GetSmallCollectionById(string smallCollectionPointId);

		Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(CollectionUnit smallCollectionPoints, string adminUsername, string adminPassword);

		Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model);

		Task<List<SmallCollectionPointsResponse>> GetSmallCollectionPointActive();
		Task<bool> UnActiveCollectionUnit(string collectionUnitId);
		Task<bool> ActiveCollectionUnit(string collectionUnitId);

	}
}

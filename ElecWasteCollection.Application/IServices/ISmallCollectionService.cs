using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ISmallCollectionService
	{
		Task<bool> AddNewSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints);
		Task<bool> UpdateSmallCollectionPoint(SmallCollectionPoints smallCollectionPoints);

		Task<bool> DeleteSmallCollectionPoint(string smallCollectionPointId);

		List<SmallCollectionPointsResponse> GetSmallCollectionPointByCompanyId(string companyId);

		SmallCollectionPointsResponse? GetSmallCollectionById(string smallCollectionPointId);

		Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword);

		Task<PagedResultModel<SmallCollectionPointsResponse>> GetPagedSmallCollectionPointsAsync(SmallCollectionSearchModel model);
	}
}

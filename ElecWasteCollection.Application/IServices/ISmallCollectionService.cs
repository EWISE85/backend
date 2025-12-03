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

		Task<bool> DeleteSmallCollectionPoint(int smallCollectionPointId);

		List<SmallCollectionPointsResponse> GetSmallCollectionPointByCompanyId(int companyId);

		SmallCollectionPointsResponse? GetSmallCollectionById(int smallCollectionPointId);

		Task<ImportResult> CheckAndUpdateSmallCollectionPointAsync(SmallCollectionPoints smallCollectionPoints, string adminUsername, string adminPassword);
	}
}

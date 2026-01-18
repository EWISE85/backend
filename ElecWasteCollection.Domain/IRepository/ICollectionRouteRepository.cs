using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICollectionRouteRepository : IGenericRepository<CollectionRoutes>
	{
		Task<(List<CollectionRoutes> Items, int TotalCount)> GetPagedRoutesAsync(string? collectionPointId,DateOnly? pickUpDate,string? status,int page,int limit);
		Task<List<CollectionRoutes>> GetRoutesByDateWithDetailsAsync(DateOnly pickUpDate);
		Task<List<CollectionRoutes>> GetRoutesByDateAndPointWithDetailsAsync(DateOnly pickUpDate,string collectionPointId);
		Task<CollectionRoutes?> GetRouteWithDetailsByIdAsync(Guid collectionRouteId);
		Task<List<CollectionRoutes>> GetRoutesByCollectorAndDateWithDetailsAsync(DateOnly pickUpDate,Guid collectorId);
    }
}

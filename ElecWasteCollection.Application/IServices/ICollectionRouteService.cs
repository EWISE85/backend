using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ICollectionRouteService
    {
        Task<List<CollectionRouteModel>> GetAllRoutes(DateOnly PickUpDate);
		Task<List<CollectionRouteModel>> GetRoutesByCollectorId(DateOnly PickUpDate, Guid collectorId);
		Task<List<CollectionRouteModel>> GetAllRoutesByDateAndByCollectionPoints(DateOnly PickUpDate, string collectionPointId);
		Task<CollectionRouteModel> GetRouteById(Guid collectionRoute);

        Task<bool> ConfirmCollection(Guid collectionRouteId, List<string> confirmImages, string QRCode);

        Task<bool> CancelCollection(Guid collectionRouteId, string rejectMessage);

        Task<bool> IsUserConfirm(Guid collectionRouteId, bool isConfirm, bool isSkip);

		Task<PagedResultModel<CollectionRouteModel>> GetPagedRoutes(RouteSearchQueryModel parameters);


	}
}

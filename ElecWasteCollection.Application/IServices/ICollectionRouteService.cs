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
        List<CollectionRouteModel> GetAllRoutes(DateOnly PickUpDate);
		CollectionRouteModel GetRouteById(Guid collectionRoute);

        bool ConfirmCollection(Guid collectionRouteId, List<string> confirmImages);

        bool CancelCollection(Guid collectionRouteId, string rejectMessage);

	}
}

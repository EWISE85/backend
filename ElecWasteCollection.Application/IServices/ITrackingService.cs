using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ITrackingService
    {
		//Task<List<CollectionTimelineModel>> GetCollectionTimelineAsync(Guid collectionRouteId);
		//Task<List<ProductHistoryModel>> GetProductHistoryAsync(Guid productId);

		Task<List<CollectionTimelineModel>> GetFullTimelineByPostIdAsync(Guid postId);
	}
}

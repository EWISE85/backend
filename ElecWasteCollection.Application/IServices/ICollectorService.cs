using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface ICollectorService
	{
		Task<bool> AddNewCollector(User collector);
		Task<bool> UpdateCollector(User collector);
		Task<bool> DeleteCollector(Guid collectorId);


		Task<CollectorResponse> GetById(Guid id);

		Task<List<CollectorResponse>> GetAll();

		Task<List<CollectorResponse>> GetCollectorByCompanyId(string companyId);

		Task<List<CollectorResponse>> GetCollectorByWareHouseId(string wareHouseId);

		Task<ImportResult> CheckAndUpdateCollectorAsync(User collector, string collectorUsername, string password);

		Task<PagedResultModel<CollectorResponse>> GetPagedCollectorsAsync(CollectorSearchModel model);
	}
}

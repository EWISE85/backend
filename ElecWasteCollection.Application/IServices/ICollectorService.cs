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


		CollectorResponse? GetById(Guid id);

		List<CollectorResponse> GetAll();

		List<CollectorResponse> GetCollectorByCompanyId(string companyId);

		List<CollectorResponse> GetCollectorByWareHouseId(string wareHouseId);

		Task<ImportResult> CheckAndUpdateCollectorAsync(User collector, string collectorUsername, string password);

		Task<PagedResultModel<CollectorResponse>> GetPagedCollectorsAsync(CollectorSearchModel model);
	}
}

using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ICollectionCompanyService
    {
		Task<bool> AddNewCompany(CollectionTeams collectionTeams);
		Task<bool> UpdateCompany(CollectionTeams collectionTeams);
		Task<bool> DeleteCompany(int collectionCompanyId);
		Task<List<CollectionCompanyResponse>> GetAllCollectionCompaniesAsync();

		Task<ImportResult> CheckAndUpdateCompanyAsync(CollectionTeams collectionTeams, string adminUsername, string password);
		CollectionCompanyResponse? GetCompanyById(int collectionCompanyId);

		Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model);

	}
}

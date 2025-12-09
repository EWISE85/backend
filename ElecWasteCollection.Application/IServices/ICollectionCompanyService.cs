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
		Task<bool> AddNewCompany(CollectionCompany collectionTeams);
		Task<bool> UpdateCompany(CollectionCompany collectionTeams);
		Task<bool> DeleteCompany(string collectionCompanyId);
		Task<List<CollectionCompanyResponse>> GetAllCollectionCompaniesAsync();

		Task<ImportResult> CheckAndUpdateCompanyAsync(CollectionCompany collectionTeams, string adminUsername, string password);
		CollectionCompanyResponse? GetCompanyById(string collectionCompanyId);

		Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model);

	}
}

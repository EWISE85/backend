using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface ICompanyService
    {
		Task<bool> AddNewCompany(Company collectionTeams);
		Task<bool> UpdateCompany(Company collectionTeams);
		Task<bool> DeleteCompany(string collectionCompanyId);
		Task<List<CollectionCompanyResponse>> GetAllCollectionCompaniesAsync();

		Task<ImportResult> CheckAndUpdateCompanyAsync(Company collectionTeams, string adminUsername, string password);
		Task<CollectionCompanyResponse?> GetCompanyById(string collectionCompanyId);

		Task<List<CollectionCompanyResponse>?> GetCompanyByName(string companyName);

		Task<PagedResultModel<CollectionCompanyResponse>> GetPagedCompanyAsync(CompanySearchModel model);

	}
}

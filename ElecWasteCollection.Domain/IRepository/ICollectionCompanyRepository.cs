using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICollectionCompanyRepository : IGenericRepository<CollectionCompany>
	{
		Task<(List<CollectionCompany> Items, int TotalCount)> GetPagedCompaniesAsync(string? status,int page,int limit);
	}
}

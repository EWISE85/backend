using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICompanyRepository : IGenericRepository<Company>
	{
		Task<(List<Company> Items, int TotalCount)> GetPagedCompaniesAsync(string? status,int page,int limit);
        Task<(List<Company> Items, int TotalCount)>GetPagedCollectionCompaniesAsync( int page, int limit);
    }
}

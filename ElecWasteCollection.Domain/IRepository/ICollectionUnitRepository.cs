using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICollectionUnitRepository : IGenericRepository<CollectionUnit>
	{
		Task<(List<CollectionUnit> Items, int TotalCount)> GetPagedAsync(string? companyId,string? status,int page,int limit);
		Task<string?> GetScpNameAsync(string scpId);
    }
}

using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ICollectorRepository : IGenericRepository<User>
	{
		Task<(List<User> Items, int TotalCount)> GetPagedCollectorsAsync(string? status,string? companyId,string? smallCollectionPointId,int page,int limit);
	}
}

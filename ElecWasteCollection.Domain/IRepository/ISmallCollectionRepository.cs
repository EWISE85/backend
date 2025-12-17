using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface ISmallCollectionRepository : IGenericRepository<SmallCollectionPoints>
	{
		Task<(List<SmallCollectionPoints> Items, int TotalCount)> GetPagedAsync(string? companyId,string? status,int page,int limit);
	}
}

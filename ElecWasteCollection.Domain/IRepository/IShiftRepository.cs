using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IShiftRepository : IGenericRepository<Shifts>
	{
		Task<(List<Shifts> Items, int TotalCount)> GetPagedShiftsAsync(string? collectionCompanyId,string? smallCollectionPointId,string? status,DateOnly? fromDate, DateOnly? toDate,int page,int limit);
		Task<Shifts?> GetShiftWithDetailsAsync(string shiftId);
	}
}

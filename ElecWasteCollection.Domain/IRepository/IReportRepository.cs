using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IReportRepository : IGenericRepository<UserReport>
	{
		Task<(List<UserReport> Items, int TotalCount)> GetPagedReport(string? type, string? status, DateOnly? start, DateOnly? end ,int page, int limit);
		Task<(List<UserReport> Items, int TotalCount)> GetPagedReportForUser(Guid? userId,string? type, string? status, DateOnly? start, DateOnly? end, int page, int limit);

	}
}

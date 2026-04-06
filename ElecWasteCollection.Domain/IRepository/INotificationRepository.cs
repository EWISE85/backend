using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface INotificationRepository :IGenericRepository<Notifications>
	{
		Task<(List<Notifications> Items, int TotalCount)> GetPagedNotificationForUser(Guid userId, int page, int limit);
	}
}

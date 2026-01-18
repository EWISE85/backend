using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IUserRepository : IGenericRepository<User>
	{
		Task<List<User>> AdminFilterUser(int page,int limit,DateOnly? fromDate,DateOnly? toDate,string? email,string? status);
	}
}

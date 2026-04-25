using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.IRepository
{
	public interface IVoucherRepository : IGenericRepository<Voucher>
	{
		Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucher(string? name, string? status, int page, int limit);
		Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucherByUser(Guid userId, string? name, string? status, int page, int limit);
		Task<(List<Voucher> Items, int TotalCount)> GetPagedVoucherForUser(
			Guid userId,
			string? name,
			string? status,
			int page,
			int limit);
	}
}

using ElecWasteCollection.Application.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface  IVoucherService
    {
        Task<PagedResultModel<VoucherModel>> GetPagedVouchers(VoucherQueryModel model);
        Task<bool> CreateVoucher(CreateVoucherModel model);

        Task<VoucherModel> GetVoucherById(Guid id);

        Task<PagedResultModel<VoucherModel>> GetPagedVouchersByUser(UserVoucherQueryModel model);

        Task<bool> UserReceiveVoucher(Guid userId, Guid voucherId);
	}
}

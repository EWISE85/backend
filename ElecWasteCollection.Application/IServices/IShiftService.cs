using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
	public interface IShiftService
	{
		Task<bool> CreateShiftAsync(CreateShiftModel newShift);
		Task<bool> UpdateShiftAsync(CreateShiftModel updateShift);

		Task<bool> DeleteShiftAsync(string shiftId);
		Task<ShiftModel> GetShiftById(string shiftId);
		Task<PagedResultModel<ShiftModel>> GetPagedShiftAsync(ShiftSearchModel model);
		Task<ImportResult> CheckAndUpdateShiftAsync(CreateShiftModel shift);

	}
}

using ElecWasteCollection.Application.Model;
using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.IServices
{
    public interface IPublicHolidayService
    {
		Task<ImportResult> CheckAndUpdatePublicHolidayAsync(PublicHoliday publicHoliday);
		Task DeactivateMissingHolidaysAsync(List<string> importedNames, ImportResult result);
		Task<List<PublicHolidayModel>> GetAllPublicHolidayActive();
	}
}

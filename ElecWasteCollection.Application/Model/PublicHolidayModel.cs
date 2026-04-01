using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PublicHolidayModel
	{
		public Guid PublicHolidayId { get; set; }
		public string Name { get; set; } = string.Empty;
		public DateOnly StartDate { get; set; }
		public DateOnly EndDate { get; set; }
	}
}

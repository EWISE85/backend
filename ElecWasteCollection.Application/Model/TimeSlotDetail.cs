using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class TimeSlotDetail
	{
		public string StartTime { get; set; }
		public string EndTime { get; set; }
	}
	public class DailyTimeSlots
	{
		public string DayName { get; set; }

		public DateOnly PickUpDate { get; set; }

		public TimeSlotDetail Slots { get; set; }
	}
}

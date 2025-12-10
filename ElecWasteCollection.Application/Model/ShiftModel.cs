using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ShiftModel
	{
		public string ShiftId { get; set; }
		public Guid CollectorId { get; set; }

		public string CollectorName { get; set; }
		public string? Vehicle_Id { get; set; }
		public string? Plate_Number { get; set; } 
		public DateOnly WorkDate { get; set; }
		public DateTime Shift_Start_Time { get; set; }
		public DateTime Shift_End_Time { get; set; }
		public string Status { get; set; }
	}
}

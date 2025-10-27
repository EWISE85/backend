using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreatePostModel
	{
		public Guid SenderId { get; set; }
		public string Name { get; set; }
		public string Category { get; set; }
		public string Description { get; set; }
		public string Time { get; set; }
		public string Address { get; set; }
		public List<string> Images { get; set; }
		public List<DailyTimeSlots> CollectionSchedule { get; set; }
	}
}

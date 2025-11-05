using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CollectionTimelineModel
	{
		public string Status { get; set; }
		public string Description { get; set; }
		//public DateTime Timestamp { get; set; }
		public string Date { get; set; }

		public string Time { get; set; }
	}
}

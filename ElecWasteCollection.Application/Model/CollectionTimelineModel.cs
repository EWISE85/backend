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

	public class ProductDetailForTracking
	{
		public string CategoryName { get; set; }
		public string Description { get; set; }

		public string BrandName { get; set; }

		public List<string> Images { get; set; }

		public string? Address { get; set; }

		public string Status { get; set; }
	}
}

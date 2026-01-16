using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ProductTrackingTimelineResponse
	{
		public ProductDetailForTracking ProductInfo { get; set; }
		public List<CollectionTimelineModel> Timeline { get; set; }
	}
}

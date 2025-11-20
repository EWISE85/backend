using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CollectionRouteModel
	{
		public Guid CollectionRouteId { get; set; }

		public Guid PostId { get; set; }
		public Guid ProductId { get; set; }

		//public string ItemName { get; set; }

		public string BrandName { get; set; }

		public string SubCategoryName { get; set; }

		public Collector Collector { get; set; }

		public User Sender { get; set; }
		public DateOnly CollectionDate { get; set; }
		public TimeOnly EstimatedTime { get; set; }
		public TimeOnly? Actual_Time { get; set; }

		public List<string> ConfirmImages { get; set; }

		public List<string> PickUpItemImages { get; set; }	

		public string LicensePlate { get; set; }

		public string Address { get; set; }



		public string Status { get; set; }
	}
}

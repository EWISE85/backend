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

		public Collector Collector { get; set; }

		public DateOnly CollectionDate { get; set; }
		public TimeOnly EstimatedTime { get; set; }
		public TimeOnly Actual_Time { get; set; }

		public List<string> ConfirmImages { get; set; }

		public string LicensePlate { get; set; }

		public string Address { get; set; }

		public string Status { get; set; }
	}
}

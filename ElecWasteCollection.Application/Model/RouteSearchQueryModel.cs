using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class RouteSearchQueryModel
	{
		public int Page { get; set; }

		public int Limit { get; set; }

		public int? CollectionPointId { get; set; }
		public DateOnly? PickUpDate { get; set; }

		public string? Status { get; set; }

	}
}

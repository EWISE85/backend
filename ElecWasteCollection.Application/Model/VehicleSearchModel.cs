using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class VehicleSearchModel
	{
		public int Page { get; set; } = 1;

		public int Limit { get; set; } = 10;

		public string? PlateNumber { get; set; }

		public string? CollectionCompanyId { get; set; }

		public string? SmallCollectionPointId { get; set; }

		public string? Status { get; set; }
	}
}

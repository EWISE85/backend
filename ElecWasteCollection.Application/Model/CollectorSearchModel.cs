using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CollectorSearchModel
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; } = 10;

		public int? CompanyId { get; set; }

		public int? SmallCollectionId { get; set; }

		public string? Status { get; set; }
	}
}

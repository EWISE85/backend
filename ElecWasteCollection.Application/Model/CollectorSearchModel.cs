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

		public string? CompanyId { get; set; }

		public string? SmallCollectionId { get; set; }

		public string? Status { get; set; }
	}
}

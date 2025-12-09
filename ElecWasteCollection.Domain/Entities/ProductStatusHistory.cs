using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class ProductStatusHistory
	{
		public Guid ProductStatusHistoryId { get; set; }

		public Guid ProductId { get; set; }

		public string Status { get; set; }

		public string StatusDescription { get; set; }

		public DateTime ChangedAt { get; set; }

		public Products Product { get; set; }

	}
}

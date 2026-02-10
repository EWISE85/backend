using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public class PackageStatusHistory
    {
		public Guid PackageStatusHistoryId { get; set; }

		public string PackageId { get; set; }

		public string Status { get; set; }

		public string StatusDescription { get; set; }

		public DateTime ChangedAt { get; set; }

		public Packages Packages { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class CollectionRouteStatusHistory
	{
		public Guid CollectionRouteStatusHitoryId { get; set; }

		public Guid CollectionRouteId { get; set; }

		public CollectionRoutes CollectionRoute { get; set; }

		public string Status { get; set; }

		public string StatusDescription { get; set; }

		public DateTime ChangedAt { get; set; }

	}
}

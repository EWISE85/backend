using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Post
	{
		public Guid PostId { get; set; }

		public Guid SenderId { get; set; }

		public Guid ProductId { get; set; }

		public string Description { get; set; }
		public DateTime Date { get; set; }
		public string? Address { get; set; }
		public string? ScheduleJson { get; set; }

		public double EstimatePoint { get; set; }

		public List<string>? CheckMessage { get; set; }
		public string? RejectMessage { get; set; }
		public string Status { get; set; }
        public string? CollectionCompanyId { get; set; }
        public string? AssignedSmallPointId { get; set; }
        public double? DistanceToPointKm { get; set; }

		public Products Product { get; set; }

		public User Sender { get; set; }

		public Company? CollectionCompany { get; set; }

		public SmallCollectionPoints? AssignedSmallPoint { get; set; }
	}
}

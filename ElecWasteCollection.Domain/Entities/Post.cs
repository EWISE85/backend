using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Post
	{
		public Guid Id { get; set; }

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
		//public ICollection<PostImages> Images { get; set; }

        public int? CollectionCompanyId { get; set; }
        public int? AssignedSmallPointId { get; set; }
        public double? DistanceToPointKm { get; set; }
    }
}

using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PostModel
	{
		public Guid Id { get; set; }

		public User Sender { get; set; }
		public string Name { get; set; }
		public string Category { get; set; }
		public string Description { get; set; }
		public DateTime Date { get; set; }
		public string Address { get; set; }
		public List<DailyTimeSlots> Schedule { get; set; }
		public List<string> Images { get; set; }
		public string Status { get; set; }
	}
}

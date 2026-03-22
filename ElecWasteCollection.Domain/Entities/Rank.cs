using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Rank
	{
		public Guid RankId { get; set; }
		public string RankName { get; set; }
		public double MinCo2 { get; set; }
		public string IconUrl { get; set; }
		public virtual ICollection<User> User { get; set; } = new List<User>();

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.CollectorStatistic
{
	public class CollectorStatisticModel
	{
		public Guid CollectorId { get; set; }
		public StatisticPeriod Period { get; set; }
		public DateTime TargetDate { get; set; } = DateTime.UtcNow;
	}
}

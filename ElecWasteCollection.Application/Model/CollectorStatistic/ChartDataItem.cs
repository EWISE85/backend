using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.CollectorStatistic
{
	public class ChartDataItem
	{
		public string Label { get; set; } = string.Empty; // Ví dụ: "T2", "T3", "Tuần 1", "Tuần 2"
		public int Value { get; set; }
	}
}

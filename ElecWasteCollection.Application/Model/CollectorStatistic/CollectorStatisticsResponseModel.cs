using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.CollectorStatistic
{
	public class CollectorStatisticsResponseModel
	{
		// Tóm tắt
		public int TotalOrders { get; set; }
		public int CompletedOrders { get; set; }
		public int FailedOrders { get; set; }

		// Biểu đồ
		public List<ChartDataItem> CompletedChart { get; set; } = new();
		public List<ChartDataItem> FailedChart { get; set; } = new();
	}
}

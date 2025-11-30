using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PostSummaryModel
	{
		public Guid Id { get; set; }
		public string Category { get; set; } // Tên Category
		public string Status { get; set; }
		public DateTime Date { get; set; }
		public string Address { get; set; }
		public string SenderName { get; set; } // Chỉ cần tên người đăng
		public string ThumbnailUrl { get; set; }

		public double EstimatePoint { get; set; }
	}
}

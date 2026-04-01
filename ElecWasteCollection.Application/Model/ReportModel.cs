using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ReportModel
	{
		public Guid ReportId { get; set; }

		public string ReportUserName { get; set; }

		public Guid? ReportRouteId { get; set; }

		public string ReportDescription { get; set; }

		public string ReportType { get; set; }

		public string? AnswerMessage { get; set; }

		public DateTime? ResolvedAt { get; set; }

		public DateTime CreatedAt { get; set; }

		public Guid ReportUserId { get; set; }

		public string Status { get; set; }

	}
}

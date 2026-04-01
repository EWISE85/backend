using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreateReportModel
	{
		public Guid UserId { get; set; }

		public Guid? CollectionRouteId { get; set; }

		public string Description { get; set; } = string.Empty;

		public string ReportType { get; set; } = string.Empty;
	}
}

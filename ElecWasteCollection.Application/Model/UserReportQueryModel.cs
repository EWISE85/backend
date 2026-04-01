using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class UserReportQueryModel
	{
		public int PageNumber { get; set; } = 1;

		public int Limit { get; set; } = 10;

		public string? Type { get; set; }

		public string? Status { get; set; }

		public DateOnly? Start { get; set; }

		public DateOnly? End { get; set; }

		public Guid? UserId { get; set; }
	}
}

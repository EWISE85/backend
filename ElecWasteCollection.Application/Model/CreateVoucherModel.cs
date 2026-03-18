using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreateVoucherModel
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string? ImageUrl { get; set; }

		public string Description { get; set; }

		public DateOnly StartAt { get; set; }

		public DateOnly EndAt { get; set; }

		public double Value { get; set; }

		public double PointsToRedeem { get; set; }
	}
}

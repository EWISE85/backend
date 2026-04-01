using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CategoryAttributeModel
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public double? MinValue { get; set; }

		public double? MaxValue { get; set; }

		public string Unit { get; set; }

		public string Status { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class AttributeOptions
	{
		public Guid OptionId { get; set; }

		public Guid AttributeId { get; set; }

		public string OptionName { get; set; }

		public double? EstimateWeight { get; set; } // (kg)

		public double? EstimateVolume { get; set; } // (m³)

		public Attributes Attribute { get; set; }
	}
}

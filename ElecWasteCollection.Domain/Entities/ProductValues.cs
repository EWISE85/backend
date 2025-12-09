using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class ProductValues
	{
		public Guid ProductValuesId { get; set; }
		public Guid ProductId { get; set; }

		public Guid? AttributeId { get; set; }

		public Guid? AttributeOptionId { get; set; }


		public double? Value { get; set; }

		public Products Product { get; set; }

		public Attributes? Attribute { get; set; }
	}
}

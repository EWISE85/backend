using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class CategoryAttributes
	{
		public Guid CategoryAttributeId { get; set; }

		public Guid CategoryId { get; set; }

		public Guid AttributeId { get; set; }

		public Category Category { get; set; }

		public Attributes Attribute { get; set; }
	}
}

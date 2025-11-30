using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Attributes
	{
		public Guid Id { get; set; }

		public string Name { get; set; }

		public virtual ICollection<AttributeOptions> AttributeOptions { get; set; }

		//public string ValueType { get; set; }
	}
}

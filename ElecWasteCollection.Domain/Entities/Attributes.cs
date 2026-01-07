using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum AttributeStatus
	{
		Active,
		Inactive
	}
	public class Attributes
	{
		public Guid AttributeId { get; set; }

		public string Name { get; set; }

		public string Status { get; set; } = AttributeStatus.Active.ToString();

		public virtual ICollection<AttributeOptions> AttributeOptions { get; set; }

		public virtual ICollection<CategoryAttributes> CategoryAttributes { get; set; }

		public virtual ICollection<ProductValues> ProductValues { get; set; }

	}
}

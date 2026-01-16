using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Brand
	{
		public Guid BrandId { get; set; }

		public string Name { get; set; }

		public Guid CategoryId { get; set; }

		public Category Category { get; set; }

		public virtual ICollection<Products> Products { get; set; } = new List<Products>();

	}
}

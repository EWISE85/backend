using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class CompanyRecyclingCategory
	{
		public string CompanyId { get; set; }

		public Guid CategoryId { get; set; }

		public Company Company { get; set; }

		public Category Category { get; set; }


	}
}

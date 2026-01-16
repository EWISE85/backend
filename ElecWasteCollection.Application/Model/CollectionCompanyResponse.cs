using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CollectionCompanyResponse
	{
		public string Id { get; set; }
		public string Name { get; set; } = null!;
		public string CompanyEmail { get; set; } = null!;
		public string Phone { get; set; } = null!;
		public string City { get; set; } = null!;
		public string Status { get; set; } = null!;
	}
}

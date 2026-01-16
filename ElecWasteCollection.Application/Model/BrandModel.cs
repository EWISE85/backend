using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class BrandModel
	{
		public Guid BrandId { get; set; }

		public string Name { get; set; }

		public Guid CategoryId { get; set; }
	}
}

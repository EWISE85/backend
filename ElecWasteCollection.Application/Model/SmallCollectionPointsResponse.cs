using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class SmallCollectionPointsResponse
	{
		public string Id { get; set; }
		public string Name { get; set; } = null!;
		public string Address { get; set; } = null!;
		public double Latitude { get; set; }
		public double Longitude { get; set; }

		public string OpenTime { get; set; } = null!;

		public string Status { get; set; } = null!;
	
		public string CompanyId { get; set; }
	}
}

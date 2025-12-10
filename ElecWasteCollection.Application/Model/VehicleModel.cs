using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class VehicleModel
	{
		public string VehicleId { get; set; }
		public string PlateNumber { get; set; } = null!;
		public string VehicleType { get; set; } = null!;
		public int CapacityKg { get; set; }
		public int CapacityM3 { get; set; }
		public int RadiusKm { get; set; }
		public string Status { get; set; } = null!;
		public string SmallCollectionPointId { get; set; }

		public string SmallCollectionPointName{ get; set; }
	}
}

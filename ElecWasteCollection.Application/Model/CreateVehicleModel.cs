using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreateVehicleModel
	{
		public string VehicleId { get; set; }
		public string Plate_Number { get; set; } = null!;
		public string Vehicle_Type { get; set; } = null!;
		public int Capacity_Kg { get; set; }
		public int Capacity_M3 { get; set; }
		public int Radius_Km { get; set; }
		public string Status { get; set; } = null!;
		public string Small_Collection_Point { get; set; }
	}
}

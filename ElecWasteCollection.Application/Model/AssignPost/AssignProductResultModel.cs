using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class AssignProductResult
    {
        public int TotalAssigned { get; set; }
        public int TotalUnassigned { get; set; }
        public List<object> Details { get; set; } = new();

		public List<WarehouseAllocationStats> WarehouseAllocations { get; set; } = new();
	}
	public class WarehouseAllocationStats
	{
		public string WarehouseId { get; set; }
		public string WarehouseName { get; set; }
		public string AdminWarehouseId { get; set; } 
		public int AssignedCount { get; set; }
	}
}

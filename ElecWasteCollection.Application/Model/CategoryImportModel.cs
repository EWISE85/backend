using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CategoryImportModel
	{
		public string Name { get; set; }
		public string ParentName { get; set; } // Cái này chỉ dùng để "dò" ID
		public double DefaultWeight { get; set; }
		public double EmissionFactor { get; set; }
	}
}

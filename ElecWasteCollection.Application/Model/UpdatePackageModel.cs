using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class UpdatePackageModel
	{
		public string PackageId { get; set; }

		public string PackageName { get; set; }

		public int SmallCollectionPointsId { get; set; }

		public List<string> ProductsQrCode { get; set; }
	}
}

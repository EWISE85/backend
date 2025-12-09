using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreatePackageModel
	{
		public string PackageId { get; set; }

		public string PackageName { get; set; }

		public string SmallCollectionPointsId { get; set; }

		public List<string> ProductsQrCode { get; set; }
	}
}

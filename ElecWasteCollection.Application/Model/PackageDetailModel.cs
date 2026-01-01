using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PackageDetailModel
	{
		public string PackageId { get; set; }

		public string SmallCollectionPointsId { get; set; }

		public string Status { get; set; }

		public List<ProductDetailModel> Products { get; set; }
	}
	
}

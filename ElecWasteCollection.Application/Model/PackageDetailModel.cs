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
        public string SmallCollectionPointsName { get; set; }
        public string SmallCollectionPointsAddress { get; set; }

		public PagedResultModel<ProductDetailModel> Products { get; set; }

		public List<PackageStatusHistoryModel> StatusHistories { get; set; }
	}
	
}

using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ProductDraftModel
	{
		public Products Product { get; set; }
		public List<ProductValues> ProductValues { get; set; }
		public List<Image> ProductImages { get; set; }
		public ProductStatusHistory History { get; set; }

		public string CategoryName { get; set; }
		public string ChildCategoryName { get; set; }
		public string BrandName { get; set; }
	}
}

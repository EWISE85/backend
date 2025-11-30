using ElecWasteCollection.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreatePostModel
	{
		public Guid SenderId { get; set; }
		//public string Name { get; set; }
		public string Description { get; set; }
		public string Address { get; set; }
		public List<string> Images { get; set; }
		public List<DailyTimeSlots> CollectionSchedule { get; set; }
		public CreateProductModel Product { get; set; }
	}
	public class CreateProductModel
	{
		public Guid ParentCategoryId { get; set; }

		public Guid SubCategoryId { get; set; }
		public Guid BrandId { get; set; }


		public List<ProductValueModel>? Attributes { get; set; }
	}
	public class ProductValueModel
	{
		public Guid AttributeId { get; set; }

		public Guid? OptionId { get; set; }

		public double? Value { get; set; }
	}
}

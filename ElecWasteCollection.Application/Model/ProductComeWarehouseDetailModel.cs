using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class ProductComeWarehouseDetailModel
	{
		public Guid ProductId { get; set; }

		public Guid CategoryId { get; set; }

		public string CategoryName { get; set; }
		public string Description { get; set; } 

		public Guid BrandId { get; set; } 
		public string BrandName { get; set; }

		public List<string> ProductImages { get; set; }

		public string? QrCode { get; set; }
		public string Status { get; set; } 

		public string? SizeTierName { get; set; }
		public double? EstimatePoint { get; set; } 
		public double? RealPoint { get; set; } 

		public DateOnly? PickUpDate { get; set; } 
	}
}

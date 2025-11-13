using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Products
	{
		public Guid Id { get; set; }
		public Guid CategoryId { get; set; }
		public Guid? SizeTierId { get; set; }
		public Guid BrandId { get; set; }

		public string? PackageId { get; set; }

		public SizeTier SizeTier { get; set; }

		public string Description { get; set; }

		public DateOnly? CreateAt { get; set; }

		public string? QRCode { get; set; }

		public string Status { get; set; }

		public int? SmallCollectionPointId { get; set; }
		public Category Category { get; set; }

		public Category Brand { get; set; }

		public Packages? Package { get; set; }
	}
}

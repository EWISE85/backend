using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
   public class ProductImages
    {
		public Guid ProductImagesId { get; set; }

		public Guid ProductId { get; set; }

		public string ImageUrl { get; set; }

		public string? AiDetectedLabelsJson { get; set; }

		public Products Product { get; set; }
	}
}

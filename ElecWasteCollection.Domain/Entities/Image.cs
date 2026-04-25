using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
   public class Image
    {
		public Guid Id { get; set; }

		public Guid? ProductId { get; set; }
		public Guid? PostId { get; set; }

		public string ImageUrl { get; set; }
		public string? AiDetectedLabelsJson { get; set; }

		public Products? Product { get; set; }
		public Post? Post { get; set; }
	}
}

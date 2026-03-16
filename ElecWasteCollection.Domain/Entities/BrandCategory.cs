using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public class BrandCategory
    {
        public Guid BrandCategoryId { get; set; }

		public Guid BrandId { get; set; }

        public Guid CategoryId { get; set; }

        public double Points { get; set; }

        public Brand Brand { get; set; }

		public Category Category { get; set; }
	}
}

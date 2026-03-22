using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Category
	{
		public Guid CategoryId { get; set; }

		public string Name { get; set; }

		public Guid? ParentCategoryId { get; set; }
		public double DefaultWeight { get; set; } = 0.0;
		public double EmissionFactor { get; set; } = 0.0;
		public  Category ParentCategory { get; set; }

		public virtual ICollection<CategoryAttributes> CategoryAttributes { get; set; }

		public virtual ICollection<Category> SubCategories { get; set; }

		public virtual ICollection<Products> Products { get; set; }

		public virtual ICollection<CompanyRecyclingCategory> CompanyRecyclingCategories { get; set; }
		public virtual ICollection<BrandCategory> BrandCategories { get; set; } = new List<BrandCategory>();

	}
}

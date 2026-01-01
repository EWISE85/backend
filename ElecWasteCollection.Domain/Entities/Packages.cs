using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public class Packages
	{
		public string PackageId { get; set; }
		public string PackageName { get; set; }
		public DateTime CreateAt { get; set; }
		public string SmallCollectionPointsId { get; set; }
		public string Status { get; set; }
        public SmallCollectionPoints SmallCollectionPoints { get; set; }
		public ICollection<Products> Products { get; set; }
	}
}

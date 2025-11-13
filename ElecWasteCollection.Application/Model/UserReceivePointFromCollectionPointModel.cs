using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class UserReceivePointFromCollectionPointModel
    {
		public Guid ProductId { get; set; }
		public string Description { get; set; }

		public double Point { get; set; }
	}
}

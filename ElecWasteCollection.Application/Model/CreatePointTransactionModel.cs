using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class CreatePointTransactionModel
	{
		public Guid? PostId { get; set; }

		public Guid UserId { get; set; }
		public string Desciption { get; set; }

		public double Point { get; set; }
	}
}

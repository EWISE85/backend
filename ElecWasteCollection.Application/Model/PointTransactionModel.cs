using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PointTransactionModel
	{
		public Guid PointTransactionId { get; set; }

		public Guid? PostId { get; set; }

		public Guid? ProductId { get; set; }

		public Guid UserId { get; set; }
		public string Desciption { get; set; }

		public string TransactionType { get; set; }

		public double Point { get; set; }

		public DateTime CreatedAt { get; set; }
	}
}

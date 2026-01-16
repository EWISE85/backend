using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class UserPointModel
	{
		public Guid Id { get; set; }

		public Guid UserId { get; set; }

		public double Points { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class NotificationModel
	{
		public Guid NotificationId { get; set; }

		public Guid UserId { get; set; }

		public string Title { get; set; }

		public string Message { get; set; }

		public DateTime CreatedAt { get; set; }

		public bool IsRead { get; set; }
	}
}

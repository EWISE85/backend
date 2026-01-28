using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum NotificationType
	{
		Event,
		System
	}
	public class Notifications
	{
		public Guid NotificationId { get; set; }

		public Guid UserId { get; set; }

		public string Title { get; set; }

		public string Body { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

		public bool IsRead { get; set; } = false;

		public Guid EventId { get; set; }

		public string Type { get; set; }

		public User User { get; set; }
	}
}

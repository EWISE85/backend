using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public class PublicHoliday
    {
		public Guid PublicHolidayId { get; set; }
		public string Name { get; set; } = string.Empty;
		public DateOnly StartDate { get; set; }

		public DateOnly EndDate { get; set; }
		public string? Description { get; set; }

		public bool IsActive { get; set; } = true;

		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
	public enum SystemConfigStatus
	{
		Active,
		Inactive
	}

	public class SystemConfig
    {
        public Guid SystemConfigId { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }

        public string DisplayName { get; set; }

		public string GroupName { get; set; }

		public string Status { get; set; }
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class SystemConfigModel
	{
		public Guid SystemConfigId { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }

		public string DisplayName { get; set; }

		public string GroupName { get; set; }
        public string? CompanyName { get; set; }
        public string? ScpName { get; set; }

        public string Status { get; set; }
	}

    public class WarehouseSpeedRequest
    {
        public string SmallCollectionPointId { get; set; }
        public double SpeedKmh { get; set; }
    }

    public class WarehouseSpeedResponse : SystemConfigModel
    {
        public string? SmallCollectionPointId { get; set; }
    }
    public class AutoGroupingSettingModel
    {
        public string CollectionUnitId { get; set; }
        public bool IsEnabled { get; set; }
        public string ScheduleTime { get; set; }
        public double LoadThresholdPercent { get; set; } 
    }

    public class AutoGroupingSettingResponse
    {
        public string CollectionUnitId { get; set; }
        public string CollectionUnitName { get; set; }
        public bool IsEnabled { get; set; }
        public string ScheduleTime { get; set; }
    }
}

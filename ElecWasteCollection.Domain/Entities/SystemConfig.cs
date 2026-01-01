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
	public enum SystemConfigKey
	{
		QR_SCAN_RADIUS_METERS,
		DAYS_TO_MARK_MISSING,
		AI_AUTO_APPROVE_THRESHOLD,
		MAX_PICKUP_DURATION_MINUTES,
		FORMAT_IMPORT_COMPANY,
		FORMAT_IMPORT_SMALLCOLLECTIONPOINT,
		FORMAT_IMPORT_COLLECTOR,
		FORMAT_IMPORT_SHIFT,
		FORMAT_IMPORT_VEHICLE,
        ASSIGN_RATIO,             
        RADIUS_KM,                
        MAX_ROAD_DISTANCE_KM,     
        SERVICE_TIME_MINUTES,     
        AVG_TRAVEL_TIME_MINUTES
    }
	public class SystemConfig
    {
        public Guid SystemConfigId { get; set; }

		public string Key { get; set; }

		public string Value { get; set; }

        public string DisplayName { get; set; }

		public string GroupName { get; set; }

		public string Status { get; set; }
        public string? CompanyId { get; set; }
        public string? SmallCollectionPointId { get; set; }

		public Company? Company { get; set; }

		public SmallCollectionPoints? SmallCollectionPoint { get; set; }
	}
}

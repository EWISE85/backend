using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum SystemConfigStatus
    {
        [Description("Đang hoạt động")]
        DANG_HOAT_DONG,

        [Description("Không hoạt động")]
        KHONG_HOAT_DONG 
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
        AVG_TRAVEL_TIME_MINUTES,
        TRANSPORT_SPEED,
        AUTO_ASSIGN_ENABLED,                // Giá trị: "true" hoặc "false"
        AUTO_ASSIGN_IMMEDIATE_THRESHOLD,    // Giá trị: "200" (Đạt ngưỡng này là chia ngay)
        AUTO_ASSIGN_SCHEDULE_TIME,          // Giá trị: "23:59" (Giờ quét hằng ngày)
        AUTO_ASSIGN_SCHEDULE_MIN_QTY,       // Giá trị: "100" (Tới giờ chốt, nếu >= 100 đơn thì chia)
        AUTO_ASSIGN_LAST_RUN_DATE,          // Giá trị: "2026-03-28" (Để tránh chia trùng trong cùng 1 phút)
        SYSTEM_ADMIN_ID,
        WAREHOUSE_LOAD_THRESHOLD,           // Giá trị mặc định ngưỡng chứa: "0.7"

        TIME_TO_CHANGE_STATUS_ROUTE
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

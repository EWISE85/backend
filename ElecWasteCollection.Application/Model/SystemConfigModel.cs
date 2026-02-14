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
        // Thuộc tính tiện ích để lấy giá trị số từ chuỗi Value
        public double SpeedKmh => double.TryParse(Value, out var v) ? v : 0;
    }
}

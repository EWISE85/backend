using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{

    public class UpdatePointSettingRequest
    {
        public string PointId { get; set; }
        public double? ServiceTimeMinutes { get; set; }
        public double? AvgTravelTimeMinutes { get; set; }
    }

    public class PointSettingDetailDto
    {
        public string SmallPointId { get; set; }
        public string SmallPointName { get; set; }
        public double ServiceTimeMinutes { get; set; }
        public double AvgTravelTimeMinutes { get; set; }
        public bool IsDefault { get; set; }
    }

    public class SinglePointSettingResponse : PointSettingDetailDto
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class AutoAssignSettings
    {
        public bool IsEnabled { get; set; }
        public int ImmediateThreshold { get; set; }
        public string ScheduleTime { get; set; } 
        public int ScheduleMinQty { get; set; }
    }
    public class UpdateAutoAssignRequest
    {
        public bool? IsEnabled { get; set; }
        public int? ImmediateThreshold { get; set; }
        public string? ScheduleTime { get; set; }
        public int? ScheduleMinQty { get; set; }
    }
}

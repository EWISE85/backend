using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class PendingPostModel
    {
        public Guid PostId { get; set; }
        public string UserName { get; set; } = "";
        public string Address { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string SizeTier { get; set; } = "";
        public double Weight { get; set; }
        public double Volume { get; set; }
        public string ScheduleJson { get; set; } = "";
        public string Status { get; set; } = "";
    }

}

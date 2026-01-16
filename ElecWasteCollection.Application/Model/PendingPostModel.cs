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
        public Guid ProductId { get; set; }
        public string UserName { get; set; } = "";
        public string Address { get; set; } = "";
        public string ProductName { get; set; } = "";
        public double Length { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string DimensionText { get; set; } = "";   
        public double Weight { get; set; }
        public double Volume { get; set; }
        public string ScheduleJson { get; set; } = "";
        public string Status { get; set; } = "";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class WarehouseLoadThresholdSettings
    {
        public Guid SystemConfigId { get; set; }
        public double Threshold { get; set; }
        public string DisplayName { get; set; }
        public string Status { get; set; }
    }

    public class UpdateThresholdRequest
    {
        public string SmallCollectionPointId { get; set; }
        public double Threshold { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class AssignDayRequest
    {
        public string CollectionPointId { get; set; }
        public DateOnly WorkDate { get; set; }
        public List<VehicleAssignmentDetail> Assignments { get; set; } = new List<VehicleAssignmentDetail>();
    }
    public class VehicleAssignmentDetail
    {
        public string VehicleId { get; set; }
        public List<Guid> ProductIds { get; set; } = new List<Guid>();
    }

    public class AssignDayResponse
    {
        public bool Success { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class AssignDayRequest
    {
        public int CollectionPointId { get; set; }
        public DateOnly WorkDate { get; set; }
        public int VehicleId { get; set; }
        public List<Guid> PostIds { get; set; } = new();
    }

    public class AssignDayResponse
    {
        public bool Success { get; set; }
    }
}

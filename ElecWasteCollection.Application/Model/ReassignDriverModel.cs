using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class ReassignDriverRequest
    {
        public int GroupId { get; set; }     
        public Guid NewCollectorId { get; set; } 
    }

    public class ReassignDriverResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int GroupId { get; set; }
        public string ShiftId { get; set; }       
        public string CollectorName { get; set; } 
        public string VehiclePlate { get; set; }  
    }

    public class ReassignCandidateDto
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
        public bool IsAvailable { get; set; }     
        public string StatusText { get; set; }
    }
}

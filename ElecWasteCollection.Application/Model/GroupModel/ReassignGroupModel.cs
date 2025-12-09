using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class ReassignGroupRequest
    {
        public int GroupId { get; set; }      
        public Guid NewCollectorId { get; set; } 
    }
    public class ReassignGroupResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int GroupId { get; set; }
        public string CollectorName { get; set; } = string.Empty;
    }

}

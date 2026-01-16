using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class AssignProductResult
    {
        public int TotalAssigned { get; set; }
        public int TotalUnassigned { get; set; }
        public List<object> Details { get; set; } = new();
    }
}

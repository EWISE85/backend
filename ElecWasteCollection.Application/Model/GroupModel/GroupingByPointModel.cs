using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.GroupModel
{
    public class GroupingByPointResponse
    {
        public string CollectionPoint { get; set; } = string.Empty;
        public bool SavedToDatabase { get; set; }

        public List<GroupSummary> CreatedGroups { get; set; } = new();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Logs { get; set; } = new List<string>();
    }

    public class GroupingByPointRequest
    {
        public string CollectionPointId { get; set; }

        public bool SaveResult { get; set; } = false;
    }
}

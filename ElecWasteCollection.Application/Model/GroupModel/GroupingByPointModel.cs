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
    }

    public class GroupingByPointRequest
    {
        public int CollectionPointId { get; set; }

        public bool SaveResult { get; set; } = false;
    }
}

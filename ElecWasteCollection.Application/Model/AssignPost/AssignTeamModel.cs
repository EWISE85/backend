using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class AssignTeamRequest
    {
        [Required]
        [MinLength(1, ErrorMessage = "At least 1 PostId is required.")]
        public List<Guid> PostIds { get; set; } = new();
    }

    public class TeamQuotaItem
    {
        public int TeamId { get; set; }
        public double Ratio { get; set; }
        public int Quota { get; set; }
    }

    public class AssignedTeamItem
    {
        public Guid PostId { get; set; }
        public int TeamId { get; set; }
        public double RatioPercent { get; set; }
        public double DistanceKm { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class UnassignedTeamItem
    {
        public Guid PostId { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class AssignTeamResult
    {
        public int Processed { get; set; }
        public List<AssignedTeamItem> Assigned { get; set; } = new();
        public List<UnassignedTeamItem> Unassigned { get; set; } = new();
    }
}

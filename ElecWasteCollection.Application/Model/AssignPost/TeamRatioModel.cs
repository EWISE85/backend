using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class TeamRatioConfigRequest
    {
        [Required]
        public List<TeamRatioItem> Teams { get; set; } = new();
    }

    public class TeamRatioItem
    {
        [Required]
        public int TeamId { get; set; }

        [Range(0, 100, ErrorMessage = "RatioPercent must be between 0 and 100.")]
        public double RatioPercent { get; set; }
    }

    public class TeamRatioConfigResponse
    {
        public string Message { get; set; } = string.Empty;
        public List<TeamRatioItem> Ratios { get; set; } = new();
    }
}

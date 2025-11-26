using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class DistanceSettingsRequest
    {
        [Range(0.1, 1000, ErrorMessage = "MaxDistanceKm must be between 0.1 and 1000.")]
        public double MaxDistanceKm { get; set; }
    }

    public class DistanceSettingsResponse
    {
        public double MaxDistanceKm { get; set; }
    }
}

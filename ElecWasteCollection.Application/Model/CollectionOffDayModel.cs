using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class RegisterOffDayRequest
    {
        public string? CompanyId { get; set; }
        public List<string> SmallCollectionPointIds { get; set; } = new();
        public List<DateOnly> OffDates { get; set; } = new();
        public string? Reason { get; set; }
    }

    public class CompanyAvailableModel
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public List<string> ActivePoints { get; set; } = new();
    }
    public class CollectionOffDayModel
    {
        public Guid Id { get; set; }
        public string? CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? SmallCollectionPointId { get; set; }
        public string? PointName { get; set; }
        public DateOnly OffDate { get; set; }
        public string? Reason { get; set; }
    }
}

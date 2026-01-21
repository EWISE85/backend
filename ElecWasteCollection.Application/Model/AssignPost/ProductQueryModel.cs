using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class CompanyDailySummaryDto
    {
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public int TotalCompanyProducts { get; set; }
        public List<SmallPointSummaryDto> Points { get; set; } = new();
    }

    public class SmallPointSummaryDto
    {
        public string SmallCollectionId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalProduct { get; set; }
    }
}

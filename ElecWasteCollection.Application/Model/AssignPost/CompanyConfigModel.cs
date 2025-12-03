using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model.AssignPost
{
    public class CompanyConfigRequest
    {
        public List<CompanyConfigItem> Companies { get; set; } = new();
    }
    public class CompanyConfigResponse
    {
        public string Message { get; set; }
        public List<CompanyConfigDto> Companies { get; set; }
    }
    public class CompanyConfigDto
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; }
        public double RatioPercent { get; set; }
        public List<SmallPointDto> SmallPoints { get; set; }
    }

    public class CompanyConfigItem
    {
        public int CompanyId { get; set; }
        public double RatioPercent { get; set; }
        public List<SmallPointConfigItem> SmallPoints { get; set; } = new();
        public int Quota { get; set; }
    }
}

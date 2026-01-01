using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
    public class RecyclingCompanyDto
    {
        public string CompanyId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
    }
    public class AssignScpToCompanyRequest
    {
        public string RecyclingCompanyId { get; set; }
        public List<string> SmallCollectionPointIds { get; set; }
    }
    public class UpdateScpAssignmentRequest
    {
        public string NewRecyclingCompanyId { get; set; }
    }

    public class CollectionCompanyGroupDto
    {
        public string CompanyId { get; set; }
        public string CompanyName { get; set; }
        public List<ScpAssignmentStatusDto> SmallPoints { get; set; } = new List<ScpAssignmentStatusDto>();
    }
    public class ScpAssignmentStatusDto
    {
        public string SmallPointId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public AssignedRecyclerDto? RecyclingCompany { get; set; }
    }
    public class AssignedRecyclerDto
    {
        public string CompanyId { get; set; }
        public string Name { get; set; }
    }
}

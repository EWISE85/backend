using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Domain.Entities
{
    public enum CompanyStatus
	{
		Active,
		Inactive,
	}
	public class CollectionTeams
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string CompanyEmail { get; set; } = null!;
        public string Phone { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
    }
}

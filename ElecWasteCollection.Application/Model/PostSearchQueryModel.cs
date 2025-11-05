using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class PostSearchQueryModel
	{
		
		public int Page { get; set; } = 1;

		
		public int Limit { get; set; } = 10;

		
		public string? Search { get; set; }

		
		public string Order { get; set; } = "DESC";
		

		public string? Status { get; set; }
	}
}

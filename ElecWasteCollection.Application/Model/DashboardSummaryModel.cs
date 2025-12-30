using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{
	public class DashboardSummaryModel
	{
		public DateOnly FromDate { get; set; }
		public DateOnly ToDate { get; set; }

		public int TotalUsers { get; set; }
		public int TotalCompanies { get; set; }
		public int TotalProducts { get; set; } 

		public List<CategoryStatisticModel> ProductCategories { get; set; }
	}

	public class CategoryStatisticModel
	{
		public string CategoryName { get; set; }
		public int Count { get; set; }
	}
}

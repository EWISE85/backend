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

	//Packages
    public class PackageDashboardResponse
    {
        public string SmallCollectionPointId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        public int TotalPackages { get; set; } 
        public List<PackageDailyStat> DailyStats { get; set; }
    }

    public class PackageDailyStat
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
        public double? PercentChange { get; set; }
    }
    //kg - m3
    public class ProductCollectDashboardResponse
    {
        public string SmallCollectionPointId { get; set; } = null!;
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public List<ProductCollectDailyStat> DailyStats { get; set; } = new();
    }

    public class ProductCollectDailyStat
    {
        public DateOnly Date { get; set; }

        public double TotalWeightKg { get; set; }
        public double TotalVolumeM3 { get; set; }

        public double? WeightPercentChange { get; set; }
        public double? VolumePercentChange { get; set; }
    }
}

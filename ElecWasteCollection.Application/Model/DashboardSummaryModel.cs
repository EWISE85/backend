using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElecWasteCollection.Application.Model
{

    // 1. Class dùng chung cho các chỉ số (User, Product, Company)
    public class MetricStats
    {
        public int CurrentValue { get; set; }    
        public int PreviousValue { get; set; }    
        public int AbsoluteChange { get; set; }    
        public double PercentChange { get; set; } 
        public string Trend { get; set; }     
    }

    public class CategoryStatisticExtendedModel : MetricStats
    {
        public string CategoryName { get; set; }
    }

    public class DashboardSummaryModel
    {
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }

        public MetricStats TotalUsers { get; set; }
        public MetricStats TotalCompanies { get; set; }
        public MetricStats TotalProducts { get; set; }

        public List<CategoryStatisticExtendedModel> ProductCategories { get; set; }
    }

    // Packages
    public class PackageDailyStat
    {
        public DateOnly Date { get; set; }
        public int Count { get; set; }
        public int? AbsoluteChange { get; set; }
        public double? PercentChange { get; set; } 
    }

    public class PackageDashboardResponse
    {
        public string SmallCollectionPointId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public MetricStats TotalPackages { get; set; }
        public List<PackageDailyStat> DailyStats { get; set; }
    }

    public class SCPDashboardSummaryModel
    {
        public string SmallCollectionPointId { get; set; }
        public DateOnly FromDate { get; set; }
        public DateOnly ToDate { get; set; }
        public MetricStats TotalProducts { get; set; }
        public List<CategoryStatisticExtendedModel> ProductCategories { get; set; }
    }




    //public class DashboardSummaryModel
    //{
    //	public DateOnly FromDate { get; set; }
    //	public DateOnly ToDate { get; set; }

    //	public int TotalUsers { get; set; }
    //	public int TotalCompanies { get; set; }
    //	public int TotalProducts { get; set; } 

    //	public List<CategoryStatisticModel> ProductCategories { get; set; }
    //}

    //public class CategoryStatisticModel
    //{
    //	public string CategoryName { get; set; }
    //	public int Count { get; set; }
    //}
}

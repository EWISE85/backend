using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ElecWasteCollection.API.DTOs.Request
{
	public class PackageSearchCompanyQueryRequest
	{
		[FromQuery(Name = "page")]
		[DefaultValue(1)]
		public int Page { get; set; } = 1;

		[FromQuery(Name = "limit")]
		[DefaultValue(10)]
		public int Limit { get; set; } = 10;

		[FromQuery(Name = "companyId")]
		public string? CompanyId { get; set; }

		[FromQuery(Name = "fromDate")]
		public DateTime? FromDate { get; set; }

		[FromQuery(Name = "toDate")]
		public DateTime? ToDate { get; set; }

		[FromQuery(Name = "status")]
		public string? Status { get; set; }

	}
}

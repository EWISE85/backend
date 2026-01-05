using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ElecWasteCollection.API.DTOs.Request
{
    public class PackageSearchRecyclerQueryRequest
    {
		[FromQuery(Name = "page")]
		[DefaultValue(1)]
		public int Page { get; set; } = 1;

		[FromQuery(Name = "limit")]
		[DefaultValue(10)]
		public int Limit { get; set; } = 10;

		[FromQuery(Name = "recyclerId")]
		public string? RecyclerId { get; set; }
		[FromQuery(Name = "status")]
		public string? Status { get; set; }
	}
}

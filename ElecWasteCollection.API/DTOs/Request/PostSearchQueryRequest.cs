using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace ElecWasteCollection.API.DTOs.Request
{
	public class PostSearchQueryRequest
	{
		[FromQuery(Name = "page")]
		[DefaultValue(1)]
		public int Page { get; set; } = 1;

		[FromQuery(Name = "limit")]
		[DefaultValue(10)]
		public int Limit { get; set; } = 10;

		[FromQuery(Name = "search")]
		public string? Search { get; set; }

		[FromQuery(Name = "order")]
		[DefaultValue("DESC")]
		public string Order { get; set; } = "DESC";
		[FromQuery(Name = "status")]
		public string? Status { get; set; }
	}
}

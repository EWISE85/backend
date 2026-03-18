namespace ElecWasteCollection.API.DTOs.Request
{
	public class UserVoucherQueryRequest
	{
		public int Page { get; set; } = 1;

		public int Limit { get; set; } = 10;

		public string? Name { get; set; }

		public string? Status { get; set; }
	}
}

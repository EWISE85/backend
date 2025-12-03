namespace ElecWasteCollection.API.DTOs.Request
{
	public class CompanySearchRequest
	{
		public int Page { get; set; } = 1;


		public int Limit { get; set; } = 10;

		public string? Status { get; set; }
	}
}

namespace ElecWasteCollection.API.DTOs.Request
{
	public class SmallCollectionSearchRequest
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; }

		public int? CompanyId { get; set; }

		public string? Status { get; set; }

	}
}

namespace ElecWasteCollection.API.DTOs.Request
{
	public class CollectorSearchRequest
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; } = 10;
		public string? CompanyId { get; set; }

		public string? SmallCollectionId { get; set; }
		public string? Status { get; set; }
	}
}

namespace ElecWasteCollection.API.DTOs.Request
{
	public class CollectorSearchRequest
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; } = 10;
		public int? CompanyId { get; set; }

		public int? SmallCollectionId { get; set; }
		public string? Status { get; set; }
	}
}

namespace ElecWasteCollection.API.DTOs.Request
{
	public class AdminFilterProductRequest
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; } = 10;

		public DateOnly? FromDate { get; set; }

		public DateOnly? ToDate { get; set; }

		public string? CategoryName { get; set; }

		public string? CollectionCompanyId { get; set; }
	}
}

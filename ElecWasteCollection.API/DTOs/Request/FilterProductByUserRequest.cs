namespace ElecWasteCollection.API.DTOs.Request
{
	public class FilterProductByUserRequest
	{
		public int Page { get; set; } = 1;
		public int Limit { get; set; } = 10;

		public string? Search { get; set; }

		public DateOnly? CreateAt { get; set; }

		public Guid UserId { get; set; }
	}
}

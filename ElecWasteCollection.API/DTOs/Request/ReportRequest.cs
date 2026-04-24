namespace ElecWasteCollection.API.DTOs.Request
{
	public class ReportRequest
	{
		public Guid UserId { get; set; }

		public Guid? ProductId { get; set; }

		public string Description { get; set; } = string.Empty;

		public string ReportType { get; set; } = string.Empty;

		public List<string>? Images { get; set; }

	}
}

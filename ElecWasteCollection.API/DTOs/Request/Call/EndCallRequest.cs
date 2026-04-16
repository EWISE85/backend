namespace ElecWasteCollection.API.DTOs.Request.Call
{
	public class EndCallRequest
	{
		public Guid PartnerId { get; set; }

		public string CallId { get; set; } = null!;
	}
}

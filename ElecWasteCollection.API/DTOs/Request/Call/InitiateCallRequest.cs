namespace ElecWasteCollection.API.DTOs.Request.Call
{
	public class InitiateCallRequest
	{
		public Guid CallerId { get; set; }
		public string CallerName { get; set; } = null!;
		public Guid CalleeId { get; set; }
		public string CallId { get; set; } = null!;
		public string RoomId { get; set; } = null!;
	}
}

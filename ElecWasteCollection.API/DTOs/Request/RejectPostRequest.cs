namespace ElecWasteCollection.API.DTOs.Request
{
	public class RejectPostRequest
	{
		public List<Guid> PostIds { get; set; }
		public string RejectMessage { get; set; }
	}
}

namespace ElecWasteCollection.API.DTOs.Request
{
    public class SendNotificationToUserRequest
    {
		public List<Guid> UserIds { get; set; }

		public string Title { get; set; }

		public string Message { get; set; }
	}
}

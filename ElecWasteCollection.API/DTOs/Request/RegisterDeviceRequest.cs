namespace ElecWasteCollection.API.DTOs.Request
{
	public class RegisterDeviceRequest
	{
		public Guid UserId { get; set; }

		public string FcmToken { get; set; }
		public string Platform { get; set; }
	}
}

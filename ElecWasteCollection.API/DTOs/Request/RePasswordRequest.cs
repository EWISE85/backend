namespace ElecWasteCollection.API.DTOs.Request
{
	public class RePasswordRequest
	{
		public string Email { get; set; }
		public string NewPassword { get; set; }
		public string ConfirmNewPassword { get; set; }

	}
}

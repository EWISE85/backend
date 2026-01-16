namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdateUserRequest
	{ 
		public string Email { get; set; }
		public string AvatarUrl { get; set; }

		public string PhoneNumber { get; set; }
	}
}

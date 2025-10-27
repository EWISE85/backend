namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdateUserRequest
	{
		public int Iat { get; set; }
		public int Ing { get; set; }

		public string? avatarUrl { get; set; }
	}
}

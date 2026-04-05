namespace ElecWasteCollection.API.DTOs.Request
{
	public class RefreshTokensRequest
	{
		public string AccessToken { get; set; } = null!;

		public string RefreshToken { get; set; } = null!;
	}
}

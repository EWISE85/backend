namespace ElecWasteCollection.API.DTOs.Request
{
    public class AppleLoginRequest
    {
		public string IdentityToken { get; set; } // Bắt buộc
		public string? FirstName { get; set; }    // Có thể null (chỉ có ở lần đầu)
		public string? LastName { get; set; }
	}
}

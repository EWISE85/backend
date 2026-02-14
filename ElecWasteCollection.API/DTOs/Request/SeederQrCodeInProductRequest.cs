namespace ElecWasteCollection.API.DTOs.Request
{
	public class SeederQrCodeInProductRequest
	{
		public List<Guid> ProductIds { get; set; }

		public List<string> QrCodes { get; set; }
	}
}

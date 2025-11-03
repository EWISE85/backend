namespace ElecWasteCollection.API.DTOs.Request
{
	public class ConfirmCollectionRequest
	{
		public string QRCode { get; set; }
		public List<string> ConfirmImages { get; set; }
	}
}

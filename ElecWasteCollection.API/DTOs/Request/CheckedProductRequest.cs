namespace ElecWasteCollection.API.DTOs.Request
{
	public class CheckedProductRequest
	{
		public string PackageId { get; set; }

		public List<string> ProductQrCode { get; set; }
	}
}

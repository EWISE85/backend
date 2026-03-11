namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdatePackageDeliveryRequest
	{
		public string DeliveryQrCode { get; set; }
		public List<string> PackageIds { get; set; } = new List<string>();
	}
}

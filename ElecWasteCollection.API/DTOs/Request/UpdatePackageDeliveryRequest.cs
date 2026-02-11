namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdatePackageDeliveryRequest
	{
		public List<string> PackageIds { get; set; } = new List<string>();
	}
}

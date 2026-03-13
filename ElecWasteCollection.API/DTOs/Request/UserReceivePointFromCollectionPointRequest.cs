namespace ElecWasteCollection.API.DTOs.Request
{
	public class UserReceivePointFromCollectionPointRequest
	{
		public string QRCode { get; set; } = null!;
		public string? Description { get; set; }

		public double? Point { get; set; }
	}
}

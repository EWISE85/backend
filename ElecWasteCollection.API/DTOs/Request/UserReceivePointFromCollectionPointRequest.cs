namespace ElecWasteCollection.API.DTOs.Request
{
	public class UserReceivePointFromCollectionPointRequest
	{
		public Guid ProductId { get; set; }
		public string Description { get; set; }

		public double Point { get; set; }
	}
}

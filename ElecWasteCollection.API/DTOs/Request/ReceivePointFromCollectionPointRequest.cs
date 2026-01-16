namespace ElecWasteCollection.API.DTOs.Request
{
	public class ReceivePointFromCollectionPointRequest
	{
		public Guid PostId { get; set; }

		public Guid UserId { get; set; }
		public string Desciption { get; set; }

		public double Point { get; set; }
	}
}

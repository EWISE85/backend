namespace ElecWasteCollection.API.DTOs.Request
{
	public class ReceiveDailyPointRequest
	{
		public Guid UserId { get; set; }

		public double Points { get; set; }
	}
}

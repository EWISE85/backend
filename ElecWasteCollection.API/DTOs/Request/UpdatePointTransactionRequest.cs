namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdatePointTransactionRequest
	{
		public double NewPointValue { get; set; }

		public string ReasonForUpdate { get; set; }
	}
}

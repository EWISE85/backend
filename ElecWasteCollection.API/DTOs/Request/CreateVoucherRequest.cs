namespace ElecWasteCollection.API.DTOs.Request
{
	public class CreateVoucherRequest
	{
		public string Code { get; set; }

		public string Name { get; set; }

		public string? ImageUrl { get; set; }

		public string Description { get; set; }

		public DateOnly StartAt { get; set; }

		public DateOnly EndAt { get; set; }

		public double Value { get; set; }
		public int Quantity { get; set; }
		public string? Status { get; set; }
		public double PointsToRedeem { get; set; }
	}
}

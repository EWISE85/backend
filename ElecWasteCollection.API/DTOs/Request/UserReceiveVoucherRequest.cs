namespace ElecWasteCollection.API.DTOs.Request
{
	public class UserReceiveVoucherRequest
	{
		public Guid UserId { get; set; }

		public Guid VoucherId { get; set; }
	}
}

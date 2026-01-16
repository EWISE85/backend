namespace ElecWasteCollection.API.DTOs.Request
{
	public class CreateUpdateUserAddressRequest
	{
		public Guid UserId { get; set; }
		public string Address { get; set; }
		public double? Iat { get; set; }

		public double? Ing { get; set; }

		public bool isDefault { get; set; }
	}
}

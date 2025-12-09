namespace ElecWasteCollection.API.DTOs.Request
{
	public class UpdateSystemConfigRequest
	{
		public Guid SystemConfigId { get; set; }

		public string Value { get; set; }
	}
}

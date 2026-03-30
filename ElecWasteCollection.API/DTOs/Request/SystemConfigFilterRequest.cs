namespace ElecWasteCollection.API.DTOs.Request
{
	public class SystemConfigFilterRequest
	{
		public string? GroupName { get; set; }
        public string? CompanyId { get; set; }
        public string? ScpId { get; set; }
    }
}

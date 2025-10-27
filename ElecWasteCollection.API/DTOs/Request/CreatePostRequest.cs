using ElecWasteCollection.Application.Model;

namespace ElecWasteCollection.API.DTOs.Request
{
    public class CreatePostRequest
    {
		public Guid SenderId { get; set; }
		public string Name { get; set; }
		public string Category { get; set; }
		public string Description { get; set; }
		public string Address { get; set; }
		public List<string> Images { get; set; }
		public List<DailyTimeSlots> CollectionSchedule { get; set; }
	}
}

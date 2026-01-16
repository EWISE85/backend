namespace ElecWasteCollection.API.DTOs.Request
    {
        public class AssignProductRequest
        {
        public string WorkDate { get; set; }
        public List<Guid> ProductIds { get; set; }
        }
    }

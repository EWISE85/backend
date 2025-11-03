namespace ElecWasteCollection.Domain.Entities
{
    public class CollectionGroups
    {
        public int Id { get; set; }
        public string Group_Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public int Shift_Id { get; set; }
        public DateTime Created_At { get; set; }
    }
}

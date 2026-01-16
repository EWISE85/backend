namespace ElecWasteCollection.Domain.Entities
{
    public class CollectionGroups
    {
        public int CollectionGroupId { get; set; }
        public string Group_Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Shift_Id { get; set; }
        public DateTime Created_At { get; set; }

        public  Shifts Shifts { get; set; } = null!;

        public virtual ICollection<CollectionRoutes> CollectionRoutes { get; set; } = new List<CollectionRoutes>();
	}
}

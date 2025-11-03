namespace ElecWasteCollection.Domain.Entities
{
    public class Shifts
    {
        public int Id { get; set; }
        public int User_Id { get; set; }
        public int Vehicle_Id { get; set; }
        public DateOnly WorkDate { get; set; }
        public DateTime Shift_Start_Time { get; set; }
        public DateTime Shift_End_Time { get; set; }
    }
}

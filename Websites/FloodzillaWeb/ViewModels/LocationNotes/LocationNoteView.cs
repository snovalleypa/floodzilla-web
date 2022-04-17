namespace FloodzillaWeb.ViewModels.LocationNotes
{
    public class LocationNoteView
    {
        public int NoteId { get; set; }
        public int? LocationId { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ModFirstName { get; set; }
        public string ModLastName { get; set; }
        public bool? Pin { get; set; }
    }
}

namespace FloodzillaWeb.Models.FzModels
{
    public class LocationNote
    {
        public int NoteId { get; set; }
        public int LocationId { get; set; }
        public string Note { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int UserId { get; set; }
        public int? ModifiedBy { get; set; }
        public bool? Pin { get; set; }
        public bool? IsDeleted { get; set; }
    }
}

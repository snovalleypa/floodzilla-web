namespace FloodzillaWeb.Models.FzModels
{
    public partial class DataSubscriptions
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string FzPostUrl { get; set; }
        public bool IsSubscribe { get; set; }
        public bool IsDeleted { get; set; }

        public virtual Users User { get; set; }
    }
}

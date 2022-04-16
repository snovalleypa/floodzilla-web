namespace FloodzillaWeb.Models.FzModels
{
    public partial class LocNotification
    {
        public int UserId { get; set; }
        public int NotifyTypeId { get; set; }
        public int ChannelTypeId { get; set; }
        public int NotifyId { get; set; }
        public int LocationId { get; set; }
        public DateTime? Level1SentOn { get; set; }
        public DateTime? Level2SentOn { get; set; }
        public DateTime? Level3SentOn { get; set; }
        public virtual Locations Location { get; set; }
        public virtual UserNotification Notify { get; set; }
    }
}

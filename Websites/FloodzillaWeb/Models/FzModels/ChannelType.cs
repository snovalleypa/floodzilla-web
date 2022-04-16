namespace FloodzillaWeb.Models.FzModels
{
    public class ChannelType
    {
        public int ChannelTypeId { get; set; }
        public string ChannelTypeName { get; set; }
        public virtual ICollection<UserNotification> UserNotifications { get; set; }
    }
}

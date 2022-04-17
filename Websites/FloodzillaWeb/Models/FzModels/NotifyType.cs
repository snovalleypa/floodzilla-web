namespace FloodzillaWeb.Models.FzModels
{
    public class NotifyType
    {
        public int NotifyTypeId { get; set; }
        public string NotifyTypeName { get; set; }

        public virtual ICollection<UserNotification> UserNotifications { get; set; }
    }
}

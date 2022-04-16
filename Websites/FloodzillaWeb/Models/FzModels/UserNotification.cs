using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FloodzillaWeb.Models.FzModels
{
    public class UserNotification
    {
        public UserNotification()
        {
            LocNotifications = new HashSet<LocNotification>();
        }
        [Required(ErrorMessage ="User is required")]
        public int UserId { get; set; }
        [Required(ErrorMessage = "Notify type is required")]
        public int NotifyTypeId { get; set; }
        [Required(ErrorMessage = "Channel type is required")]
        public int ChannelTypeId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int NotifyId { get; set; }
        public bool IsActive { get; set; }
        public virtual ChannelType ChannelType { get; set; }
        public virtual NotifyType NotifyType { get; set; }
        public virtual Users User { get; set; }
        public virtual ICollection<LocNotification> LocNotifications { get; set; }

    }
}

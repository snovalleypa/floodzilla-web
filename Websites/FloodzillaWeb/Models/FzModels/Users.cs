using System.Diagnostics;

using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    [DebuggerDisplay("Name = {FirstName} {LastName}")]
    public partial class Users : UserBase
    {
        public Users()
        {
            UserLocations = new HashSet<UserLocations>();
        }
        public virtual DataSubscriptions DataSubscriptions { get; set; }
        public virtual AspNetUsers AspNetUser { get; set; }
        public virtual Organizations? Organizations { get; set; }
        public ICollection<UserLocations> UserLocations { get; set; }
        public virtual ICollection<UserNotification> UserNotifications { get; set; }
    }
}

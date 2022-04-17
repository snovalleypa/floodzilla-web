using Microsoft.AspNetCore.Identity;

namespace FloodzillaWeb.Models
{
    // This is to work around the use of UserId in old ASPNET schema vs ApplicationUserId
    // in new schema.
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public ApplicationUserRole() : base()
        {
        }

        public string ApplicationUserId
        {
            get { return this.UserId; }
            set { this.UserId = value; }
        }
    }
}

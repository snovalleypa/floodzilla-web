using FzCommon;

namespace FloodzillaWeb.Models.FzModels
{
    public partial class Organizations : OrganizationBase
    {
        public Organizations()
        {
            Regions = new HashSet<Regions>();
            Users = new HashSet<Users>();
        }

        public virtual ICollection<Regions> Regions { get; set; }
        public virtual ICollection<Users> Users { get; set; }
    }
}

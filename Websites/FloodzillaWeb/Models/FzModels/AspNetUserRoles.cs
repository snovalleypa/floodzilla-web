namespace FloodzillaWeb.Models.FzModels
{
    public partial class AspNetUserRoles
    {
        public string ApplicationUserId { get; set; }
        public string RoleId { get; set; }

        public virtual AspNetRoles Role { get; set; }
        public virtual AspNetUsers User { get; set; }
    }
}

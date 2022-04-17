namespace FloodzillaWeb.ViewModels.Users
{
    public class UsersViewModel
    {
        public int Id { get; set; }
        public string AspNetUserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Address { get; set; }
        public bool IsDeleted { get; set; }

        public string Email { get; set; }
        public string UserName { get; set; }
        public string RoleId { get; set; }
        public string RoleName { get; set; }
        public int OrgId { get; set; }
        public string OrgName { get; set; }
        public DateTime? CreatedOn { get; set; }
    }
}

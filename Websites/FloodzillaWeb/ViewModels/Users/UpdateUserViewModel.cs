using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.ViewModels.Users
{
    public class UpdateUserViewModel
    {
        public string AspNetUserId { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]        
        public string Email { get; set; }

        [Required(ErrorMessage = "First Name is required."), StringLength(20, MinimumLength = 3, ErrorMessage = "First name should be between 3-20 characters long.")]
        [Display(Name = "First Name")]
        [RegularExpression("^[a-zA-Z0-9_.-]*$", ErrorMessage = "Invalid")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required."), StringLength(20,MinimumLength =3, ErrorMessage = "Last name should be between 3-20 characters long.")]
        [Display(Name = "Last Name")]
        [RegularExpression("^[a-zA-Z0-9_.-]*$", ErrorMessage = "Invalid")]
        public string LastName { get; set; }

        //[Required(ErrorMessage = "Address is required")]
        //[Display(Name = "Address")]
        //public string Address1 { get; set; }

        public string? RoleId { get; set; }

        [Required(ErrorMessage = "Role is required.")]
        [Display(Name = "Role")]
        public string RoleName { get; set; }

        [Display(Name = "Organization")]
        public int? OrganizationsID { get; set; }

        public int Uid { get; set; }

        public string oldRole { get; set; }

    }
}

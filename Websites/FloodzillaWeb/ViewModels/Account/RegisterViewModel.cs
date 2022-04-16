using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.ViewModels.Account
{
    public class RegisterViewModel
    {
        public string AspNetUserId { get; set; }
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        [Required(ErrorMessage ="First Name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }
        //[Required(ErrorMessage = "Address is required")]
        //[Display(Name = "Address")]
        //public string Address { get; set; }
        public string RoleId { get; set; }
        [Required(ErrorMessage = "Role is required")]
        [Display(Name = "Role")]
        public string RoleName { get; set; }
        [Display(Name = "Organization")]
        public int? OrganizationsID { get; set; }
        public int Uid { get; set; }
    }
}

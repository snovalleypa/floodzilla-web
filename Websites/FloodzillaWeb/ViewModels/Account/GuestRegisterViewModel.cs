using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.ViewModels.Account
{
    public class GuestRegisterViewModel
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
        [Required(ErrorMessage = "First Name is required"), StringLength(20, MinimumLength = 3, ErrorMessage = "First name should be between 3-20 characters long.")]
        [Display(Name = "First Name")]
        [RegularExpression("^[a-zA-Z0-9_.-]*$", ErrorMessage = "Invalid")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required"), StringLength(20, MinimumLength = 3, ErrorMessage = "Last name should be between 3-20 characters long.")]
        [Display(Name = "Last Name")]
        [RegularExpression("^[a-zA-Z0-9_.-]*$", ErrorMessage = "Invalid")]
        public string LastName { get; set; }
        //[Required(ErrorMessage = "Address is required")]
        //[Display(Name = "Address")]
        //public string Address { get; set; }
    }
}

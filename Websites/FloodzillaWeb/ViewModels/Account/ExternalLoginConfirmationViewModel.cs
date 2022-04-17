using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.ViewModels.Account
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        public string LastName { get; set; }
        //public string Address { get; set; }
    }
}

﻿using System.ComponentModel.DataAnnotations;

namespace FloodzillaWeb.ViewModels.Account
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}

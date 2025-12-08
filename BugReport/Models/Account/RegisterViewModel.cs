using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace BugReport.Models.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "email-required")]
        [EmailAddress]
        [Display(Name = "email", Prompt = "email")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "login-name-required")]
        [RegularExpression(@"^[A-Z0-9]{6}$", ErrorMessage = "login-name-regex")]
        [Display(Name = "login-name", Prompt = "login-name")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "first-name-required")]
        [Display(Name = "first-name", Prompt = "first-name")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "last-name-required")]
        [Display(Name = "last-name", Prompt = "last-name")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "password")]
        [MinLength(8, ErrorMessage = "password-regex")]
        [DataType(DataType.Password)]
        [Display(Name = "password", Prompt = "password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "confirm-password-required")]
        [DataType(DataType.Password)]
        [Display(Name = "confirm-password", Prompt = "confirm-password")]
        [Compare(nameof(Password), ErrorMessage = "password-not-match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}

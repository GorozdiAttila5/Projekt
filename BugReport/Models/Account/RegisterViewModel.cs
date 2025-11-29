using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace BugReport.Models.Account
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "{0} is required")]
        [EmailAddress]
        [Display(Name = "Email address", Prompt = "Email address")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [RegularExpression(@"^[A-Z0-9]{6}$", ErrorMessage = "{0} must be 6 characters")]
        [Display(Name = "Login name", Prompt = "Login name")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "First name", Prompt = "First name")]
        public required string FirstName { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "Last name", Prompt = "Last name")]
        public required string LastName { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [MinLength(8, ErrorMessage = "{0} must be at least {1} characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Password")]
        public required string Password { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password", Prompt = "Confirm password")]
        [Compare("Password", ErrorMessage = "Password does not match")]
        public required string ConfirmPassword { get; set; }
    }
}

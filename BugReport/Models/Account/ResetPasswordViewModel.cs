using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class ResetPasswordViewModel
    {
        public required string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required")]
        [MinLength(8, ErrorMessage = "{0} must be at least {1} characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "{0} is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password", Prompt = "Confirm password")]
        [Compare("Password", ErrorMessage = "Password does not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
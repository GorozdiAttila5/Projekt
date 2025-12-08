using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class ResetPasswordViewModel
    {
        public required string Id { get; set; } = string.Empty;

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
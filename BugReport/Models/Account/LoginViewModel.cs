using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "login-name-required")]
        [RegularExpression(@"^[A-Z0-9]{6}$", ErrorMessage = "login-name-regex")]
        [Display(Name = "login-name", Prompt = "login-name")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "password-required")]
        [DataType(DataType.Password)]
        [Display(Name = "password", Prompt = "password")]
        public required string Password { get; set; }

        [Display(Name = "remember-me")]
        public bool RememberMe { get; set; }
    }
}

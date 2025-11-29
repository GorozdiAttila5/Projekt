using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "{0} is required")]
        [RegularExpression(@"^[A-Z0-9]{6}$", ErrorMessage = "{0} must be 6 characters")]
        [Display(Name = "Login name", Prompt = "Login name")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password", Prompt = "Password")]
        public required string Password { get; set; }

        [Display(Name = "Remember me?")]
        public bool RememberMe { get; set; }
    }
}

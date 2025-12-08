using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "email-required")]
        [EmailAddress]
        [Display(Name = "email", Prompt = "email")]
        public required string Email { get; set; }
    }
}

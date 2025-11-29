using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Account
{
    public class VerifyEmailViewModel
    {
        [Required(ErrorMessage = "{0} is required")]
        [EmailAddress]
        [Display(Name = "Email address", Prompt = "Email address")]
        public required string Email { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Report
{
    public class AddMessageViewModel
    {
        [Required]
        public required Guid ReportId { get; set; }

        [Required]
        [Display(Name = "Message", Prompt = "Add a message...")]
        public string Text { get; set; } = string.Empty;
    }
}

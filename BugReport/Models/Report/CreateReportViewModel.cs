using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Report
{

    public class CreateReportViewModel
    {
        [Required(ErrorMessage = "title-required")]
        [MaxLength(125, ErrorMessage = "title-regex")]
        [Display(Name = "title", Prompt = "title")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "assignee-required")]
        [Display(Name = "assignee", Prompt = "Assignee")]
        public required List<string> Assignees { get; set; }

        [Required(ErrorMessage = "description-required")]
        [Display(Name = "description", Prompt = "description")]
        public required string Description { get; set; }

        [Display(Name = "attachments", Prompt = "attachments")]
        public List<IFormFile>? Attachments { get; set; }
    }
}
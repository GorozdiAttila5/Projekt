using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Report
{

    public class CreateReportViewModel
    {
        [Required(ErrorMessage = "{0} is required")]
        [MaxLength(125, ErrorMessage = "The {0} cannot exceed {1} charaters")]
        [Display(Name = "Title", Prompt = "Title")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Please give atleat one assignee")]
        public required List<string> Assignees { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "Desctiption", Prompt = "Description")]
        public required string Description { get; set; }

        [Display(Name = "Attachments", Prompt = "Attachments")]
        public List<IFormFile>? Attachments { get; set; }
    }
}
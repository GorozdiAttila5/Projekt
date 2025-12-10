using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Report
{
    public class EditReportViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [MaxLength(125, ErrorMessage = "The {0} cannot exceed {1} charaters")]
        [Display(Name = "title", Prompt = "title")]
        public required string Title { get; set; }


        [MinLength(1, ErrorMessage = "assignee-required")]
        [Display(Name = "assignee", Prompt = "Assignee")]
        public List<string> Assignees { get; set; } = new();

        [Required(ErrorMessage = "description-required")]
        [Display(Name = "description", Prompt = "description")]
        public required string Description { get; set; }

        [Display(Name = "existing-attachments", Prompt = "existing-attachments")]
        public List<AttachmentViewModel>? Attachments { get; set; } = new();
        public List<Guid>? ExistingAttachments { get; set; } = new();

        [Display(Name = "new-attachments", Prompt = "new-attachments")]
        public List<IFormFile>? NewAttachments { get; set; } = new();

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }


    public class AttachmentViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
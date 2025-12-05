using BugReport.Entities;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BugReport.Models.Report
{
    public class EditReportViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "{0} is required")]
        [MaxLength(125, ErrorMessage = "The {0} cannot exceed {1} charaters")]
        [Display(Name = "Title", Prompt = "Title")]
        public required string Title { get; set; } = string.Empty;


        [Required(ErrorMessage = "Please give atleat one assignee")]
        public required List<string> Assignees { get; set; } = new();

        [Required(ErrorMessage = "{0} is required")]
        [Display(Name = "Desctiption", Prompt = "Description")]
        public required string Description { get; set; } = string.Empty;

        public List<AttachmentViewModel> Attachments { get; set; } = new();
        public List<Guid> ExistingAttachments { get; set; } = new();
        public List<IFormFile> NewAttachments { get; set; } = new();

        [Display(Name = "Status", Prompt = "Status")]
        public Guid StatusId { get; set; }
    }


    public class AttachmentViewModel
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
    }
}
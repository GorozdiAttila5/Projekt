using BugReport.Entities;

namespace BugReport.Models.Report
{
    public class EditReportViewModel
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> Assignees { get; set; } = new();

        // New uploads
        public List<IFormFile>? Attachments { get; set; }

        // Existing attachments (just for displaying)
        public List<Attachment>? UploadedAttachments { get; set; }

        // IDs user decided to keep
        public List<Guid>? KeepAttachmentIds { get; set; } = new();
    }

}
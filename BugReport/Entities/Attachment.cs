namespace BugReport.Entities
{
    public class Attachment
    {
        public Guid Id { get; set; }

        public Guid ReportId { get; set; } = default!;
        public Report Report { get; set; } = default!;

        public string FilePath { get; set; } = string.Empty;    
        public string FileName { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
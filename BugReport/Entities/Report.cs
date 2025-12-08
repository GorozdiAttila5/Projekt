using System.ComponentModel.DataAnnotations;

namespace BugReport.Entities
{
    public class Report
    {
        public Guid Id { get; set; }

        public string ReporterId { get; set; } = default!;
        public User Reporter {  get; set; } = default!;
        
        public ICollection<User> Assignees { get; set; } = new List<User>();

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ChangeLog>? ChangeLogs { get; set; }
        public ICollection<Attachment>? Attachments { get; set; }
        public ICollection<Message>? Messages { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }
}
using BugReport.Entities.BugReport.Entities;
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

        public ICollection<ReportArchive>? ArchivedByUsers { get; set; }

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    }

    namespace BugReport.Entities
    {
        public class ReportArchive
        {
            public Guid Id { get; set; }

            public Guid ReportId { get; set; }
            public Report Report { get; set; } = default!;

            public string UserId { get; set; } = default!;
            public User User { get; set; } = default!;

            public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
        }
    }

}
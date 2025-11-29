namespace BugReport.Entities
{
    public class ChangeLog
    {
        public Guid Id { get; set; }

        public Guid ReportId { get; set; } = default!;
        public Report Report { get; set; } = default!;

        public Guid StatusId { get; set; } = default!;
        public Status Status { get; set; } = default!;

        public string UserId { get; set; } = default!;
        public User User { get; set; } = default!;

        public string? ChangeDescription { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
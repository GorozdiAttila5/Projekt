namespace BugReport.Entities
{
    public class Message
    {
        public Guid Id { get; set; }

        public Guid ReportId { get; set; } = default!;
        public Report Report { get; set; } = default!;

        public string UserId { get; set; } = default!;
        public User User { get; set; } = default!;

        public string Text { get; set; } = string.Empty;
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;

    }
}
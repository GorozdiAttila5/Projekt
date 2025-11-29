namespace BugReport.Entities
{
    public class Status
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NormalizedName { get; set; } = string.Empty;

        public ICollection<ChangeLog>? ChangeLogs { get; set; }
    }
}
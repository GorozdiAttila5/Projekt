using BugReport.Entities;

namespace BugReport.Models.Report
{
    public class ReportViewModel
    {
        public Entities.Report Report { get; set; } = null!;
        public ChangeLog? LatestChangeLog { get; set; }
        public List<ChangeLog> ChangeLogs { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
    }
}
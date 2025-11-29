using BugReport.Entities;

namespace BugReport.Models.Report
{
    public class ReportDetailsViewModel
    {
        public Entities.Report Report { get; set; } = null!;
        public Entities.ChangeLog? LatestChangeLog { get; set; }
        public List<ChangeLog> ChangeLogs { get; set; } = new();
        public List<Message> Messages { get; set; } = new();
    }
}

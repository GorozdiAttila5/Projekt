using BugReport.Entities.BugReport.Entities;
using Microsoft.AspNetCore.Identity;

namespace BugReport.Entities
{
    public class User : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public ICollection<Report>? ReportsReported {  get; set; }
        public ICollection<Report>? ReportsAssigned { get; set; }
        public ICollection<ReportArchive>? ArchivedReports { get; set; }
        public ICollection<ChangeLog>? ChangeLogs { get; set; }
        public ICollection<Message>? Messages { get; set; }
    }
}
using BugReport.Entities;

namespace BugReport.Models.Admin
{
    public class UserRoleViewModel
    {
        public User User { get; set; } = new User();
        public IList<string> Roles { get; set; } = new List<string>();
    }
}

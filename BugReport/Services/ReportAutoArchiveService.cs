using BugReport.Data;
using BugReport.Entities;
using BugReport.Entities.BugReport.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugReport.Services
{
    public class ReportAutoArchiveService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval = TimeSpan.FromDays(1);

        public ReportAutoArchiveService
        (
            IServiceScopeFactory scopeFactory
        )
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ArchiveOldReportsAsync();
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task ArchiveOldReportsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var cutoffDate = DateTime.UtcNow.AddDays(-30);

            // Find reports whose latest change is older than 30 days
            var reports = await context.Reports
                .Include(r => r.ChangeLogs)
                .Include(r => r.Assignees)
                .Where(r =>
                    r.ChangeLogs!
                        .OrderByDescending(cl => cl.Timestamp)
                        .Select(cl => cl.Timestamp)
                        .FirstOrDefault() < cutoffDate
                )
                .ToListAsync();

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<User>>();

            var admins = await userManager.GetUsersInRoleAsync("Admin");


            foreach (var report in reports)
            {
                // Reporter
                await ArchiveForUser(context, report.Id, report.ReporterId);

                // Assignees
                foreach (var assignee in report.Assignees)
                {
                    await ArchiveForUser(context, report.Id, assignee.Id);
                }

                // Assignees
                foreach (var admin in admins)
                {
                    await ArchiveForUser(context, report.Id, admin.Id);
                }
            }

            await context.SaveChangesAsync();
        }

        private static async Task ArchiveForUser(
            ApplicationDbContext context,
            Guid reportId,
            string userId)
        {
            var exists = await context.ReportArchives
                .AnyAsync(a => a.ReportId == reportId && a.UserId == userId);

            if (!exists)
            {
                context.ReportArchives.Add(new ReportArchive
                {
                    ReportId = reportId,
                    UserId = userId
                });
            }
        }
    }
}
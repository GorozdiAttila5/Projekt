using BugReport.Entities;
using BugReport.Entities.BugReport.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace BugReport.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<User>(options)
    {
        public DbSet<Report> Reports { get; set; } = null!;
        public DbSet<Status> Statuses { get; set; } = null!;
        public DbSet<ChangeLog> ChangeLogs { get; set; } = null!;
        public DbSet<Attachment> Attachments { get; set; } = null!;
        public DbSet<Message> Messages { get; set; } = null!;
        public DbSet<ReportArchive> ReportArchives { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsReported)
                .HasForeignKey(r => r.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Report>()
                .HasMany(r => r.Assignees)
                .WithMany(u => u.ReportsAssigned)
                .UsingEntity(j =>
                {
                    j.ToTable("ReportAssignees");
                });

            builder.Entity<ChangeLog>()
                .HasOne(c => c.Report)
                .WithMany(r => r.ChangeLogs)
                .HasForeignKey(c => c.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ChangeLog>()
                .HasOne(c => c.User)
                .WithMany(u => u.ChangeLogs)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ChangeLog>()
                .HasOne(c => c.Status)
                .WithMany(s => s.ChangeLogs)
                .HasForeignKey(c => c.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attachment>()
                .HasOne(a => a.Report)
                .WithMany(r => r.Attachments)
                .HasForeignKey(a => a.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.Report)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.ReportId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<ReportArchive>()
                .HasOne(ra => ra.Report)
                .WithMany(r => r.ArchivedByUsers)
                .HasForeignKey(ra => ra.ReportId);

            builder.Entity<ReportArchive>()
                .HasOne(ra => ra.User)
                .WithMany(u => u.ArchivedReports)
                .HasForeignKey(ra => ra.UserId);
        }
    }
}
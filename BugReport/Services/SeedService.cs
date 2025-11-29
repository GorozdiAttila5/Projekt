using BugReport.Data;
using BugReport.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BugReport.Services
{
    public class SeedService
    {
        private static async Task AddRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if(!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                    throw new Exception($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        private static async Task AddStatusAsync(ApplicationDbContext context, string name)
        {
            string normalized = name.ToUpper().Replace(" ", "_");

            if (!await context.Statuses.AnyAsync(s => s.NormalizedName == normalized))
            {
                context.Statuses.Add(new Status
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    NormalizedName = normalized
                });

                await context.SaveChangesAsync();
            }
        }

        public static async Task SeedDatabaseAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            try
            {
                await context.Database.EnsureCreatedAsync();

                await AddRoleAsync(roleManager, "Student");
                await AddRoleAsync(roleManager, "Instructor");
                await AddRoleAsync(roleManager, "Admin");

                await AddStatusAsync(context, "Incoming");
                await AddStatusAsync(context, "In progress");
                await AddStatusAsync(context, "Resolved");
                await AddStatusAsync(context, "Rejected");
                await AddStatusAsync(context, "Blocked");
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}

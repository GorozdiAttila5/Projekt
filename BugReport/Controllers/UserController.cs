using BugReport.Data;
using BugReport.Entities;
using BugReport.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BugReport.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public UserController
        (
            ApplicationDbContext context,
            UserManager<User> userManager
        )
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var currentUserId = _userManager.GetUserId(User);

            var instructors = await (
                from user in _userManager.Users.AsNoTracking()
                join userRole in _context.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId
                join role in _context.Roles.AsNoTracking()
                    on userRole.RoleId equals role.Id
                where role.Name == "Instructor" && user.Id != currentUserId
                select new
                {
                    id = user.Id,
                    fullName = user.UserName + ", " + user.FirstName + " " + user.LastName
                }
            ).ToListAsync();

            return Json(instructors);
        }


    }
}
